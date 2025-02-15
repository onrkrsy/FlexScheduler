using Microsoft.Extensions.Options;
using FlexScheduler.Models;
using System.Text;
using Newtonsoft.Json;

namespace FlexScheduler.Services;

public class TokenService : ITokenService
{
    private string _currentToken = string.Empty;
    private DateTime _tokenExpiration;
    private readonly HttpClient _httpClient;
    private readonly LoginSettings _loginSettings;
    private readonly object _lockObject = new();

    public TokenService(IHttpClientFactory httpClientFactory, IOptions<LoginSettings> loginSettings)
    {
        _httpClient = httpClientFactory.CreateClient("LoginService");
        _loginSettings = loginSettings.Value;
    }

    public async Task<string> GetTokenAsync()
    {
        if (IsTokenValid())
            return _currentToken;

        lock (_lockObject)
        {
            if (IsTokenValid())
                return _currentToken;

            return RefreshTokenAsync().Result;
        }
    }

    public bool IsTokenValid()
    {
        return !string.IsNullOrEmpty(_currentToken) && _tokenExpiration > DateTime.UtcNow.AddMinutes(5);
    }

    public async Task<string> RefreshTokenAsync()
    {
        try
        {
            var loginRequest = new
            {
                ClientId = _loginSettings.ClientId,
                ClientSecret = _loginSettings.ClientSecret
            };

            var content = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_loginSettings.LoginEndpoint, content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(result);

            if (tokenResponse?.Token == null)
            {
                throw new InvalidOperationException("Token response is invalid");
            }

            _currentToken = tokenResponse.Token;
            _tokenExpiration = DateTime.UtcNow.AddMinutes(tokenResponse.ExpiresInMinutes);

            return _currentToken;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to refresh token", ex);
        }
    }
}

public class LoginSettings
{
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string LoginEndpoint { get; set; } = null!;
}

public class TokenResponse
{
    public string Token { get; set; } = null!;
    public int ExpiresInMinutes { get; set; }
}