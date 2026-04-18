using Google.Apis.Auth;

namespace DotLearn.Auth.Services.Interfaces;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken);
}
