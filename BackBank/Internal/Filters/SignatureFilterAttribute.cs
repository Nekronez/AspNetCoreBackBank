using BackBank.Models.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace BackBank.Internal.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SignatureFilterAttribute : ActionFilterAttribute, IAsyncAuthorizationFilter
    {
        public virtual Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var services = context.HttpContext.RequestServices;
            var logger = services.GetRequiredService<ILogger<SignatureFilterAttribute>>();

            var options = new SignatureValidationOptions
            {
                VerifyRequest = true
            };

            if (!VerifySignature(context.HttpContext, options, out var signature, out var reason))
            {
                logger.LogWarning(reason);

                context.Result = new StatusCodeResult(461);
            }

            return Task.CompletedTask;
        }

        protected virtual bool VerifySignature(HttpContext context, SignatureValidationOptions validationOptions, out RequestSignature signature, out string reason)
        {
            var services = context.RequestServices;
            var options = services.GetRequiredService<TransportSecuritySettings>();

            signature = context.Request.GetSignature();
            reason = string.Empty;

            if (signature == null)
            {
                reason = "Request signature not present";
                return !options.SignatureRequred;
            }

            if (validationOptions.VerifyRequest && !signature.IsValid)
            {
                reason = "Request signature not valid";
                return false;
            }

            if (validationOptions.VerifyCertificate &&
                !VerifyCertificate(signature.Token, signature.Certificate, out reason))
            {
                return false;
            }

            return true;
        }

        public virtual bool VerifyCertificate(JwtSecurityToken token, X509Certificate2 certificate, out string reason)
        {
            // Get embedded certificate
            if (certificate == null)
            {
                reason = "Certificate missing in signature token";
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new X509SecurityKey(certificate)
            };

            try
            {
                tokenHandler.ValidateToken(
                    token.RawData,
                    validationParameters,
                    out var validatedToken);
            }
            catch (Exception ex)
            when (ex is ArgumentException || ex is SecurityTokenValidationException)
            {
                reason = $"Signature validation failed ({ex.Message})";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public virtual bool VerifyThumbprint(X509Certificate2 certificate, string thumbprint, out string reason)
        {
            var incomingThumbprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);
            if (!incomingThumbprint.Equals(thumbprint, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Signature thumbprint mismatch. Expected: {thumbprint} Got: {incomingThumbprint}";

                // Temporarily disabled
                return true;
            }

            reason = string.Empty;
            return true;
        }

        protected class SignatureValidationOptions
        {
            public bool VerifyRequest { get; set; }

            public bool VerifyCertificate { get; set; }

            public string CertificateThumbprint { get; set; }

            public bool VerifyFingerprint { get; set; }

            public string Fingerprint { get; set; }

            public bool VerifyRuntimeChecks { get; set; }

            public bool VerifyThumbprint { get; set; }
        }
    }
}
