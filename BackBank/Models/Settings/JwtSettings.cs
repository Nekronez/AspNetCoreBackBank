using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace BackBank.Models.Settings
{
    public class JwtSettings
    {
        private X509Certificate2 _certificate;

        public CertificateSettings Certificate { get; set; }

        internal X509Certificate2 GetCertificate()
        {
            if (Certificate == null)
            {
                throw new InvalidOperationException("JWT Certificate not configured");
            }

            if (_certificate == null)
            {
                try
                {
                    _certificate = new X509Certificate2(Certificate.Path, Certificate.Password);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("JWT Certificate configuration not valid", ex);
                }
            }

            return _certificate;
        }

        internal SecurityKey GetSecurityKey()
        {
            var certificate = GetCertificate();

            return new X509SecurityKey(certificate);
        }
    }
}
