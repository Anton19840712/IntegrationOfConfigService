using System.Net;
using Microsoft.AspNetCore.Http;

public class IpAddressHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IpAddressHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return "unknown";
        }

        // 1. Предпочтительный заголовок X-Forwarded-For, добавляемый большинством reverse proxy.
        // Он может содержать цепочку IP: client, proxy1, proxy2... Нам нужен первый.
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            var firstIp = xForwardedFor.Split(',').Select(ip => ip.Trim()).FirstOrDefault();
            if (IPAddress.TryParse(firstIp, out var parsedIp))
            {
                // Сразу нормализуем и возвращаем
                return NormalizeIpAddress(parsedIp);
            }
        }
        
        // 2. Заголовок X-Real-IP как альтернатива.
        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp) && IPAddress.TryParse(xRealIp, out var parsedRealIp))
        {
            return NormalizeIpAddress(parsedRealIp);
        }

        // 3. Если заголовков нет, используем IP-адрес прямого подключения.
        // Это может быть IP reverse proxy или IP пользователя, если нет прокси.
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            return NormalizeIpAddress(remoteIp);
        }

        return "unknown";
    }

    /// <summary>
    /// Нормализует IP-адрес, преобразуя IPv4-mapped IPv6 в чистый IPv4.
    /// </summary>
    private string NormalizeIpAddress(IPAddress ipAddress)
    {
        // Если адрес является IPv4, представленным в формате IPv6 (например, ::ffff:10.0.0.2),
        // он будет преобразован в обычный IPv4 (10.0.0.2).
        if (ipAddress.IsIPv4MappedToIPv6)
        {
            return ipAddress.MapToIPv4().ToString();
        }

        // Для всех остальных случаев (чистый IPv4 или IPv6) возвращаем как есть.
        return ipAddress.ToString();
    }
}