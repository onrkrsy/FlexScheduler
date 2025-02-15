namespace FlexScheduler.Services;

public interface IHttpJob
{
    Task<string> Execute(
        string url,
        string method,
        string? payload,
        string? displayName,
        Dictionary<string, string>? headers = null,
        int? timeoutInSeconds = null,
        bool requiresAuthentication = true,
        CancellationToken cancellationToken = default);
} 