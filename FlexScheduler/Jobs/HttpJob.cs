using System.ComponentModel;
using System.Text;
using FlexScheduler.Services;
using Microsoft.Extensions.Logging;

namespace FlexScheduler.Jobs;

public class HttpJob : IHttpJob
{
    private readonly ILogger<HttpJob> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;
    private const int DEFAULT_TIMEOUT_SECONDS = 60;

    public HttpJob(
        ILogger<HttpJob> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ITokenService tokenService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _tokenService = tokenService;
    }

    private HttpClient CreateHttpClient(int? timeoutInSeconds = null)
    {
        var client = _httpClientFactory.CreateClient();
        var configTimeout = _configuration.GetValue<int?>("HttpClientSettings:DefaultTimeoutSeconds") ?? DEFAULT_TIMEOUT_SECONDS;
        client.Timeout = TimeSpan.FromSeconds(timeoutInSeconds ?? configTimeout);
        return client;
    }

    [DisplayName("{3}")]
    public async Task<string> Execute(
        string url,
        string method,
        string? payload,
        string? displayName,
        Dictionary<string, string>? headers = null,
        int? timeoutInSeconds = null,
        bool requiresAuthentication = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting HTTP job: {Method} {Url}", method, url);

            headers ??= new Dictionary<string, string>();

            if (requiresAuthentication)
            {
                var token = await _tokenService.GetTokenAsync();
                headers["Authorization"] = $"Bearer {token}";
                _logger.LogInformation("Authentication token added to request");
            }
            else
            {
                _logger.LogInformation("Authentication not required for this request");
            }

            using var httpClient = CreateHttpClient(timeoutInSeconds);
            using var request = new HttpRequestMessage(new HttpMethod(method), url);

            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (!string.IsNullOrEmpty(payload))
            {
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            }

            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "HTTP job completed. Status: {StatusCode}, Response: {Content}",
                response.StatusCode,
                content
            );

            response.EnsureSuccessStatusCode();
            return content;
        }
        catch (TaskCanceledException)
        {
            var configTimeout = _configuration.GetValue<int?>("HttpClientSettings:DefaultTimeoutSeconds") ?? DEFAULT_TIMEOUT_SECONDS;
            var timeoutValue = timeoutInSeconds ?? configTimeout;
            _logger.LogError("HTTP job timed out after {Timeout} seconds: {Method} {Url}", timeoutValue, method, url);
            throw new TimeoutException($"Request timed out after {timeoutValue} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP job failed: {Method} {Url}", method, url);
            throw;
        }
    }
} 