using System.Text.Json.Serialization;

namespace API.Services.Eureka;

public class EurekaRegistrationRequest
{
    [JsonPropertyName("instance")]
    public EurekaInstance Instance { get; set; } = new();
}

public class EurekaInstance
{
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = string.Empty;

    [JsonPropertyName("hostName")]
    public string HostName { get; set; } = string.Empty;

    [JsonPropertyName("app")]
    public string App { get; set; } = string.Empty;

    [JsonPropertyName("ipAddr")]
    public string IpAddr { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "UP";

    [JsonPropertyName("port")]
    public PortInfo Port { get; set; } = new();

    [JsonPropertyName("securePort")]
    public PortInfo SecurePort { get; set; } = new();

    [JsonPropertyName("healthCheckUrl")]
    public string HealthCheckUrl { get; set; } = string.Empty;

    [JsonPropertyName("statusPageUrl")]
    public string StatusPageUrl { get; set; } = string.Empty;

    [JsonPropertyName("homePageUrl")]
    public string HomePageUrl { get; set; } = string.Empty;

    [JsonPropertyName("vipAddress")]
    public string VipAddress { get; set; } = string.Empty;

    [JsonPropertyName("secureVipAddress")]
    public string SecureVipAddress { get; set; } = string.Empty;

    [JsonPropertyName("dataCenterInfo")]
    public DataCenterInfo DataCenterInfo { get; set; } = new();
}

public class PortInfo
{
    [JsonPropertyName("$")]
    public int Value { get; set; }

    [JsonPropertyName("@enabled")]
    public string Enabled { get; set; } = "true";
}

public class DataCenterInfo
{
    [JsonPropertyName("@class")]
    public string Class { get; set; } = "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "MyOwn";
}
