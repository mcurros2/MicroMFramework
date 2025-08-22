using Microsoft.AspNetCore.Http;

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
        public (string device_id, string? ipaddress, string? user_agent) GetDeviceID(string local_device_id)
        {
            var httpc = _contextAccessor.HttpContext;
            var userAgent = httpc?.Request.Headers.UserAgent.ToString();

            var ipAddress = httpc?.Connection.RemoteIpAddress?.ToString();

            return (device_id: local_device_id, ipAddress, userAgent);
        }


    }
}
