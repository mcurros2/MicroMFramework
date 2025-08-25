using Microsoft.AspNetCore.Http;

namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Provides browser-based device identification used to correlate authentication attempts
    /// with client metadata such as IP address and user agent.
    /// </summary>
    public class BrowserDeviceIDService : IDeviceIdService
    {

        private IHttpContextAccessor _contextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserDeviceIDService"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor used to obtain request information for device identification.</param>
        public BrowserDeviceIDService(IHttpContextAccessor httpContextAccessor)
        {
            _contextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Builds a device identifier from the current HTTP context.
        /// </summary>
        /// <param name="local_device_id">Identifier provided by the client.</param>
        /// <returns>A tuple containing the device identifier, client IP address, and user agent string.</returns>
        public (string device_id, string? ipaddress, string? user_agent) GetDeviceID(string local_device_id)
        {
            var httpc = _contextAccessor.HttpContext;
            var userAgent = httpc?.Request.Headers.UserAgent.ToString();

            var ipAddress = httpc?.Connection.RemoteIpAddress?.ToString();

            return (device_id: local_device_id, ipAddress, userAgent);
        }


    }
}
