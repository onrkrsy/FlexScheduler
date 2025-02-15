using Microsoft.Extensions.Logging;

namespace FlexScheduler.Jobs;

public abstract class BaseScheduledJob
{
    protected readonly ILogger _logger;
    protected readonly IHttpClientFactory _httpClientFactory;

    protected BaseScheduledJob(ILogger logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public abstract string JobId { get; }
    public virtual string Queue { get; set; } = "default";
    public virtual string ServerName { get; set; } = "default-server";

    public abstract Task Execute(CancellationToken cancellationToken);

    protected async Task ExecuteWithLogging(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{JobId} started at: {Time} UTC", JobId, DateTime.UtcNow);

        try
        {
            await action(cancellationToken);
            _logger.LogInformation("{JobId} completed successfully at: {Time} UTC", JobId, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing {JobId}", JobId);
            throw;
        }
    }
}

public abstract class BaseRecurringJob : BaseScheduledJob
{
    protected BaseRecurringJob(ILogger logger, IHttpClientFactory httpClientFactory) 
        : base(logger, httpClientFactory)
    {
    }

    public abstract string CronExpression { get; }
}

public abstract class BaseDelayedJob : BaseScheduledJob
{
    protected BaseDelayedJob(ILogger logger, IHttpClientFactory httpClientFactory) 
        : base(logger, httpClientFactory)
    {
    }

    public abstract TimeSpan Delay { get; }
} 