namespace Application.Interfaces.Service
{
    public interface ITotpService
    {
        string GenerateSecretKey();
        string GenerateQrCodeUri(string email, string secretKey, string issuer);
        byte[] GenerateQrCode(string uri);
        bool ValidateOtp(string secretKey, string otp);
    }
}
