using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Solar.Models.Siseli;

public class SiseliApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("localMessage")]
    public string? LocalMessage { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonIgnore]
    public bool IsSuccess => Code == 0;
}

public class LoginData
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("accessTokenWillExpiredAt")]
    public string? AccessTokenWillExpiredAt { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("refreshTokenWillExpiredAt")]
    public string? RefreshTokenWillExpiredAt { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("account")]
    public string? Account { get; set; }
}

public class RefreshTokenData
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("accessTokenWillExpiredAt")]
    public string? AccessTokenWillExpiredAt { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("refreshTokenWillExpiredAt")]
    public string? RefreshTokenWillExpiredAt { get; set; }
}

public class StationListResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("list")]
    public List<StationItem> List { get; set; } = new();
}

public class StationItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }
}

public class DeviceListResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("list")]
    public List<DeviceItem> List { get; set; } = new();
}

public class DeviceItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("stationId")]
    public string? StationId { get; set; }

    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("softwareVersion")]
    public string? SoftwareVersion { get; set; }

    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; set; }
}

public class TimeSeriesResponse
{
    [JsonPropertyName("payload")]
    public TimeSeriesPayload? Payload { get; set; }
}

public class TimeSeriesPayload
{
    [JsonPropertyName("timeSeries")]
    public List<string> TimeSeries { get; set; } = new();

    [JsonPropertyName("fields")]
    public Dictionary<string, List<double?>> Fields { get; set; } = new();
}

public class EnergyFlowResponse
{
    [JsonPropertyName("deviceAttributeState")]
    public EnergyFlowDeviceState? DeviceAttributeState { get; set; }
}

public class EnergyFlowDeviceState
{
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, EnergyFlowFieldItem> Fields { get; set; } = new();
}

public class EnergyFlowFieldItem
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("valueDisplay")]
    public string? ValueDisplay { get; set; }
}
