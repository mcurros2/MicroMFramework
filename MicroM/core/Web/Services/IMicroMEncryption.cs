using System.Security.Cryptography.X509Certificates;

namespace MicroM.Web.Services
{
    public interface IMicroMEncryption
    {
        public X509Certificate2? GetCertificate();

        public string Decrypt(string base64_encrypted);

        public string Encrypt(string plaintext);

    }
}
