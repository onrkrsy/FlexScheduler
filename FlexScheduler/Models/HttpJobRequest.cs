using Newtonsoft.Json.Linq;

namespace FlexScheduler.Models;

public class HttpJobRequest
{
    public string JobId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;
    public object? Payload { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Queue { get; set; } = "default";
    public string ServerName { get; set; } = "default-server";
    public int? TimeoutInSeconds { get; set; }
    public bool RequiresAuthentication { get; set; } = true;
}

public class RecurringHttpJobRequest : HttpJobRequest
{
    public string CronExpression { get; set; } = null!;
}

public class DelayedHttpJobRequest : HttpJobRequest
{
    public TimeSpan Delay { get; set; }
}

public class JobConfiguration
{
    public List<JobConfigurationItem> Jobs { get; set; } = new();
}

public class JobConfigurationItem : RecurringHttpJobRequest
{
    public bool IsEnabled { get; set; } = true;
    public List<string> Tags { get; set; } = new();
    public string? Description { get; set; }
} 