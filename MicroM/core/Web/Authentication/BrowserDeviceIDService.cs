using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace MicroM.Web.Authentication
{
    public class BrowserDeviceIDService : IDeviceIdService
    {

        private IHttpContextAccessor _contextAccessor;

        public BrowserDeviceIDService(IHttpContextAccessor httpContextAccessor)
        {
            _contextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// The device ID constructed from the HttpContext
        /// </summary>
        /// <returns></returns>
        public (string device_id, string? ipaddress, string? user_agent) GetDeviceID()
        {
            var httpc = _contextAccessor.HttpContext;
            var userAgent = httpc?.Request.Headers["User-Agent"].ToString();

            var ipAddress = httpc?.Connection.RemoteIpAddress?.ToString();

            var fingerprint = $"{ipAddress}-{userAgent}";

            // Generate a SHA-256 hash based on the fingerprint
            var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));
            var hash = Convert.ToHexString(hashedBytes).ToLowerInvariant();

            return (hash, ipAddress, userAgent);
        }


    }
}
