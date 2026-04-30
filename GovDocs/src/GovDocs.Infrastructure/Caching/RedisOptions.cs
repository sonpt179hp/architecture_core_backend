using StackExchange.Redis;

namespace GovDocs.Infrastructure.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 6379;

    public string? Password { get; set; }

    public int Database { get; set; } = 0;

    public bool Ssl { get; set; }

    public string? InstanceName { get; set; }

    public int ConnectTimeoutMs { get; set; } = 5000;

    public bool AbortOnConnectFail { get; set; } = false;

    public ConfigurationOptions ToConfigurationOptions()
    {
        var options = new ConfigurationOptions
        {
            EndPoints = { { Host, Port } },
            DefaultDatabase = Database,
            Ssl = Ssl,
            ConnectTimeout = ConnectTimeoutMs,
            AbortOnConnectFail = AbortOnConnectFail
        };

        if (!string.IsNullOrWhiteSpace(Password))
        {
            options.Password = Password;
        }

        return options;
    }
}
