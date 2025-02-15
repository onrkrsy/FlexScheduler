using Hangfire.Dashboard;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using FlexScheduler.Models;

namespace FlexScheduler.Extensions;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly HangfireSettings _settings;

    public HangfireAuthorizationFilter(IOptions<HangfireSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var header = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Basic "))
        {
            httpContext.Response.Headers.Append("WWW-Authenticate", "Basic");
            httpContext.Response.StatusCode = 401;
            return false;
        }

        var authValues = GetAuthenticationValues(header);
        if (string.IsNullOrEmpty(authValues.userName) || string.IsNullOrEmpty(authValues.password))
        {
            return false;
        }

        return authValues.userName.Equals(_settings.UserName, StringComparison.OrdinalIgnoreCase) &&
               authValues.password.Equals(_settings.Password, StringComparison.OrdinalIgnoreCase);
    }

    private static (string userName, string password) GetAuthenticationValues(string authHeader)
    {
        try
        {
            var authHeaderValue = AuthenticationHeaderValue.Parse(authHeader);
            var bytes = Convert.FromBase64String(authHeaderValue.Parameter ?? string.Empty);
            var credentials = Encoding.UTF8.GetString(bytes).Split(':', 2);
            return credentials.Length == 2 ? (credentials[0], credentials[1]) : (string.Empty, string.Empty);
        }
        catch
        {
            return (string.Empty, string.Empty);
        }
    }
} 