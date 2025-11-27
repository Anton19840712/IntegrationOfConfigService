using Application.Interfaces.Service;
using OtpNet;
using QRCoder;

namespace Application.Services
{
    public class TotpService : ITotpService
    {
        public string GenerateSecretKey()
        {
            var secretKey = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(secretKey);
        }

        public string GenerateQrCodeUri(string email, string secretKey, string issuer)
        {
            return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
        }

        public byte[] GenerateQrCode(string uri)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }

        public bool ValidateOtp(string secretKey, string otp)
        {
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(otp))
                return false;

            var totp = new Totp(Base32Encoding.ToBytes(secretKey));

            // Позволяем использовать код из текущего и предыдущего временного окна,
            // чтобы компенсировать возможную рассинхронизацию времени [web:16, web:23].
            return totp.VerifyTotp(otp, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        }
    }
}
