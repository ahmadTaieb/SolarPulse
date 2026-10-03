using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Solar.Models.Siseli;
using Solar.Options;

namespace Solar.Services.Auth;

/// <summary>
/// Thread-safe singleton provider managing token authentication, refresh, and cryptographic signing.
/// </summary>
public class SolarTokenProvider : ISolarTokenProvider, IDisposable
{
    private const string AppId = "rBrTRfAPXz";
    private const string EncryptedAppSecret = "I4D0KRr2339z3pQ/at91V9BpFAOe54DaTafwSm6suIQ=";
    private const int TokenRefreshLeadSeconds = 300; // Proactive refresh 5 minutes before expiry

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SolarPlatformOptions _options;
    private readonly ILogger<SolarTokenProvider> _logger;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset? _accessTokenExpiresUtc;
    private DateTimeOffset? _refreshTokenExpiresUtc;
    private string? _decryptedSecret;
    private bool _disposed;

    public SolarTokenProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<SolarPlatformOptions> options,
        ILogger<SolarTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (IsTokenValid())
        {
            return _accessToken!;
        }

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            if (IsTokenValid())
            {
                return _accessToken!;
            }

            // Strategy 1: Attempt refresh if refreshToken is available and not expired
            if (!string.IsNullOrEmpty(_refreshToken) &&
                _refreshTokenExpiresUtc.HasValue &&
                _refreshTokenExpiresUtc.Value > DateTimeOffset.UtcNow)
            {
                try
                {
                    _logger.LogInformation("SolarTokenProvider: Proactively refreshing access token...");
                    await RefreshAccessTokenAsync(cancellationToken);
                    return _accessToken!;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SolarTokenProvider: Refresh token failed. Falling back to account login.");
                }
            }

            // Strategy 2: Full authentication with account credentials
            _logger.LogInformation("SolarTokenProvider: Authenticating for {Email}...", _options.Email);
            await LoginAsync(cancellationToken);
            return _accessToken!;
        }
        finally
        {
            _authLock.Release();
        }
    }

    public void InvalidateToken()
    {
        _logger.LogInformation("SolarTokenProvider: Invalidating cached access token.");
        _accessToken = null;
    }

    private bool IsTokenValid()
    {
        if (string.IsNullOrEmpty(_accessToken) || !_accessTokenExpiresUtc.HasValue)
        {
            return false;
        }

        return DateTimeOffset.UtcNow < (_accessTokenExpiresUtc.Value - TimeSpan.FromSeconds(TokenRefreshLeadSeconds));
    }

    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        var client = CreateAuthHttpClient();

        string passwordMd5 = ComputeMd5Hex(_options.Password);
        var payloadObj = new { account = _options.Email, password = passwordMd5 };
        string jsonBody = JsonSerializer.Serialize(payloadObj);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

        string bodyHash = ComputeSha256Hex(bodyBytes);
        string nonce = GenerateNonce();
        string secret = GetOrDecryptAppSecret();
        string sign = ComputeIotSign(AppId, nonce, bodyHash, secret);

        using var request = new HttpRequestMessage(HttpMethod.Post, "apis/login/account");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Origin", _options.BaseUrl);
        request.Headers.Add("Referer", _options.BaseUrl.TrimEnd('/') + "/");
        request.Headers.Add("IOT-Open-AppID", AppId);
        request.Headers.Add("IOT-Open-Nonce", nonce);
        request.Headers.Add("IOT-Open-Body-Hash", bodyHash);
        request.Headers.Add("IOT-Open-Sign", sign);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<SiseliApiResponse<LoginData>>(cancellationToken: cancellationToken);
        if (apiResponse == null || !apiResponse.IsSuccess || apiResponse.Data == null)
        {
            throw new InvalidOperationException($"Solar platform login failed: {apiResponse?.Message ?? "Unknown error"} (Code: {apiResponse?.Code})");
        }

        _accessToken = apiResponse.Data.AccessToken;
        _refreshToken = apiResponse.Data.RefreshToken;
        _accessTokenExpiresUtc = ParseIsoUtc(apiResponse.Data.AccessTokenWillExpiredAt);
        _refreshTokenExpiresUtc = ParseIsoUtc(apiResponse.Data.RefreshTokenWillExpiredAt);

        _logger.LogInformation("SolarTokenProvider: Authentication successful. Access token expires at {Expiry} UTC.", _accessTokenExpiresUtc);
    }

    private async Task RefreshAccessTokenAsync(CancellationToken cancellationToken)
    {
        var client = CreateAuthHttpClient();
        var payload = new { refreshToken = _refreshToken };

        using var request = new HttpRequestMessage(HttpMethod.Post, "apis/login/refresh/access/token");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Origin", _options.BaseUrl);
        request.Headers.Add("Referer", _options.BaseUrl.TrimEnd('/') + "/");
        request.Content = JsonContent.Create(payload);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Solar platform token refresh failed with HTTP status {response.StatusCode}.");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<SiseliApiResponse<RefreshTokenData>>(cancellationToken: cancellationToken);
        if (apiResponse == null || !apiResponse.IsSuccess || apiResponse.Data == null)
        {
            throw new InvalidOperationException($"Solar platform token refresh failed: {apiResponse?.Message ?? "Unknown error"}");
        }

        _accessToken = apiResponse.Data.AccessToken;
        _refreshToken = apiResponse.Data.RefreshToken ?? _refreshToken;
        _accessTokenExpiresUtc = ParseIsoUtc(apiResponse.Data.AccessTokenWillExpiredAt);
        _refreshTokenExpiresUtc = ParseIsoUtc(apiResponse.Data.RefreshTokenWillExpiredAt);

        _logger.LogInformation("SolarTokenProvider: Token refresh successful. Next expiry: {Expiry} UTC.", _accessTokenExpiresUtc);
    }

    private HttpClient CreateAuthHttpClient()
    {
        var client = _httpClientFactory.CreateClient("SolarPlatformAuth");
        if (client.BaseAddress == null)
        {
            client.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }
        return client;
    }

    private string GetOrDecryptAppSecret()
    {
        if (!string.IsNullOrEmpty(_decryptedSecret))
        {
            return _decryptedSecret;
        }

        byte[] appIdUtf8 = Encoding.UTF8.GetBytes(AppId);
        byte[] md5Hash = MD5.HashData(appIdUtf8);
        string md5Hex = Convert.ToHexString(md5Hash).ToLowerInvariant();

        byte[] key = Encoding.ASCII.GetBytes(md5Hex[..16]);
        byte[] iv = Encoding.ASCII.GetBytes(md5Hex[16..32]);
        byte[] cipherBytes = Convert.FromBase64String(EncryptedAppSecret);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.Zeros;

        using var decryptor = aes.CreateDecryptor();
        byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        _decryptedSecret = Encoding.UTF8.GetString(plainBytes).TrimEnd('\0');

        return _decryptedSecret;
    }

    private static string ComputeIotSign(string appId, string nonce, string bodyHash, string secret)
    {
        string qs = $"IOT-Open-AppID={appId}&IOT-Open-Body-Hash={bodyHash}&IOT-Open-Nonce={nonce}";
        string b64Qs = Convert.ToBase64String(Encoding.UTF8.GetBytes(qs));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        byte[] hmacBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(b64Qs));
        byte[] signBytes = MD5.HashData(hmacBytes);

        return Convert.ToHexString(signBytes).ToLowerInvariant();
    }

    private static string ComputeMd5Hex(string input)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeSha256Hex(byte[] input)
    {
        byte[] hash = SHA256.HashData(input);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GenerateNonce()
    {
        byte[] bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static DateTimeOffset? ParseIsoUtc(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return null;
        if (DateTimeOffset.TryParse(iso, out var dto)) return dto.ToUniversalTime();
        return null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _authLock.Dispose();
            _disposed = true;
        }
    }
}
