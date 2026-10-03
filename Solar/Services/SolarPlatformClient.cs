using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Solar.Models.Siseli;
using Solar.Options;
using Solar.Services.Auth;

namespace Solar.Services;

/// <summary>
/// HTTP client implementation for communicating with the Solar of Things cloud platform.
/// </summary>
public class SolarPlatformClient : ISolarPlatformClient
{
    private static readonly string[] TimeSeriesKeys =
    [
        "pvPower",
        "pvInputPower",
        "outputActivePower",
        "acOutputActivePower",
        "mainsPower",
        "gridPower",
        "acInputVoltage",
        "gridVoltage",
        "batteryCapacity",
        "batterySOC",
        "batteryVoltage",
        "batteryDischargeCurrent",
        "batteryChargingCurrent"
    ];

    private readonly HttpClient _httpClient;
    private readonly ISolarTokenProvider _tokenProvider;
    private readonly SolarPlatformOptions _options;
    private readonly ILogger<SolarPlatformClient> _logger;

    public SolarPlatformClient(
        HttpClient httpClient,
        ISolarTokenProvider tokenProvider,
        IOptions<SolarPlatformOptions> options,
        ILogger<SolarPlatformClient> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _options = options.Value;
        _logger = logger;

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }
    }

    public async Task<List<StationItem>> GetStationsAsync(CancellationToken cancellationToken = default)
    {
        var requestBody = new { page = 1, count = 50 };
        var response = await SendAuthorizedPostAsync<SiseliApiResponse<StationListResponse>>(
            "apis/station/list",
            requestBody,
            cancellationToken);

        return response?.Data?.List ?? [];
    }

    public async Task<List<DeviceItem>> GetDevicesAsync(string? stationId = null, CancellationToken cancellationToken = default)
    {
        object requestBody = !string.IsNullOrWhiteSpace(stationId)
            ? new { page = 1, count = 50, stationId }
            : new { page = 1, count = 50 };

        var response = await SendAuthorizedPostAsync<SiseliApiResponse<DeviceListResponse>>(
            "apis/device/list",
            requestBody,
            cancellationToken);

        return response?.Data?.List ?? [];
    }

    public async Task<TimeSeriesPayload?> GetTimeSeriesDataAsync(
        string deviceId,
        DateTimeOffset fromTime,
        DateTimeOffset toTime,
        CancellationToken cancellationToken = default)
    {
        string fromFormatted = fromTime.ToString("yyyy-MM-ddTHH:mm:sszzz");
        string toFormatted = toTime.ToString("yyyy-MM-ddTHH:mm:sszzz");

        var requestBody = new
        {
            deviceId,
            count = 2000,
            page = 1,
            fromTime = fromFormatted,
            toTime = toFormatted,
            orderByTimeAsc = true,
            keys = TimeSeriesKeys
        };

        var response = await SendAuthorizedPostAsync<SiseliApiResponse<TimeSeriesResponse>>(
            "apis/deviceState/simple/attribute/keys/history/v1",
            requestBody,
            cancellationToken);

        return response?.Data?.Payload;
    }

    public async Task<EnergyFlowDeviceState?> GetLatestEnergyFlowAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        var path = $"apis/deviceState/simple/energy/flow/v1?deviceId={deviceId}&dataSource=1";
        var response = await SendAuthorizedGetAsync<SiseliApiResponse<EnergyFlowResponse>>(path, cancellationToken);

        return response?.Data?.DeviceAttributeState;
    }

    private async Task<T?> SendAuthorizedPostAsync<T>(string endpoint, object body, CancellationToken cancellationToken)
    {
        var response = await ExecuteWithTokenRetryAsync(async (token) =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
            ApplyAuthorizedHeaders(req, token);
            req.Content = JsonContent.Create(body);
            return await _httpClient.SendAsync(req, cancellationToken);
        }, cancellationToken);

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private async Task<T?> SendAuthorizedGetAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        var response = await ExecuteWithTokenRetryAsync(async (token) =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, endpoint);
            ApplyAuthorizedHeaders(req, token);
            return await _httpClient.SendAsync(req, cancellationToken);
        }, cancellationToken);

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private void ApplyAuthorizedHeaders(HttpRequestMessage request, string token)
    {
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Origin", _options.BaseUrl);
        request.Headers.Add("Referer", _options.BaseUrl.TrimEnd('/') + "/");
        request.Headers.Add("IOT-Token", token);
        request.Headers.Add("IOT-Time-Zone", _options.TimeZone);
    }

    private async Task<HttpResponseMessage> ExecuteWithTokenRetryAsync(
        Func<string, Task<HttpResponseMessage>> sendFunc,
        CancellationToken cancellationToken)
    {
        string token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        var response = await sendFunc(token);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("SolarPlatformClient: Received 401 Unauthorized. Invalidating token and retrying...");
            _tokenProvider.InvalidateToken();
            token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
            response = await sendFunc(token);
        }

        response.EnsureSuccessStatusCode();
        return response;
    }
}
