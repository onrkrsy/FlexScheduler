namespace FlexScheduler.Services;

public interface ITokenService
{
    Task<string> GetTokenAsync();
    bool IsTokenValid();
    Task<string> RefreshTokenAsync();
} 