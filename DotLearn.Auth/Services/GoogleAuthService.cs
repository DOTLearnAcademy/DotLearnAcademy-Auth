using DotLearn.Auth.Services.Interfaces;
using Google.Apis.Auth;

namespace DotLearn.Auth.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken)
    {
        var clientId = _configuration["GoogleAuth:ClientId"];

        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Google ClientId is not configured.");

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        };

        return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
    }
}
