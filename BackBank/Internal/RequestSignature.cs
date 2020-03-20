using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BackBank.Internal
{
    public class RequestSignature
    {
        private const string MethodField = "method";
        private const string QueryHashField = "request";
        private const string BodyHashField = "body";
        private const string FingerprintField = "fingerprint";
        private const string RuntimeChecksField = "device";

        private ILogger<RequestSignature> _logger;

        public RequestSignature(HttpRequest request, JwtSecurityToken token)
        {
            Token = token;

            _logger = request.HttpContext.RequestServices.GetRequiredService<ILogger<RequestSignature>>();

            var claims = token.Claims.ToDictionary(x => x.Type, x => x.Value);

            if (claims.TryGetValue(MethodField, out var method))
            {
                Method = method;
            }

            if (claims.TryGetValue(QueryHashField, out var query))
            {
                QueryHash = query;
            }

            if (claims.TryGetValue(BodyHashField, out var body))
            {
                BodyHash = body;
            }

            if (claims.TryGetValue(FingerprintField, out var fingerprint))
            {
                Fingerprint = fingerprint;
            }

            if (claims.TryGetValue(RuntimeChecksField, out var runtimeChecks))
            {
                RuntimeChecks = runtimeChecks;
            }

            // Get embedded certificate
            if (!TryGetEmbeddedCertificate(out var certificate))
            {
                Certificate = null;
            }

            Certificate = certificate;

            IsValid = VerifyRequest(request);
        }

        public RequestSignature()
        {
            IsValid = false;
        }

        /// <summary>
        /// Raw JWS token
        /// </summary>
        public JwtSecurityToken Token { get; }

        /// <summary>
        /// Request method
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// Hash of query parameters
        /// </summary>
        public string QueryHash { get; }

        /// <summary>
        /// Hash of request body
        /// </summary>
        public string BodyHash { get; }

        /// <summary>
        /// Device fingerprint
        /// </summary>
        public string Fingerprint { get; }

        /// <summary>
        /// Device runtime checks result
        /// </summary>
        public string RuntimeChecks { get; }

        /// <summary>
        /// Is valid for current request
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Request signature certificate
        /// </summary>
        public X509Certificate2 Certificate { get; }

        public bool VerifyFingerprint(string fingerprint)
        {
            return fingerprint.Equals(Fingerprint);
        }

        protected bool VerifyRequest(HttpRequest request)
        {
            // Let us seek the stream
            if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
            }

            byte[] bodyBytes;

            using (MemoryStream ms = new MemoryStream())
            {
                request.Body.CopyTo(ms);
                bodyBytes = ms.ToArray();
            }

            request.Body.Seek(0L, SeekOrigin.Begin);

            var queryString = request.QueryString.Value;
            var method = request.Method;

            if (!method.Equals(Method, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogTrace($"Signature \"method\" field should be {Method}");
                return false;
            }

            if (!(string.IsNullOrEmpty(queryString) && string.IsNullOrEmpty(QueryHash)) &&
                !ComputeHash(Encoding.UTF8.GetBytes(queryString.Substring(1))).Equals(QueryHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogTrace($"Signature \"query\" field should be {ComputeHash(Encoding.UTF8.GetBytes(queryString.Substring(1)))}");
                return false;
            }

            if (!(bodyBytes.Length == 0 && string.IsNullOrEmpty(BodyHash)) &&
                !ComputeHash(bodyBytes).Equals(BodyHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogTrace($"Signature \"body\" field should be {ComputeHash(bodyBytes)}");
                return false;
            }

            return true;
        }

        private static string ComputeHash(byte[] input)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(input);
                return BinaryUtils.ToHexString(hash);
            }
        }

        public bool TryGetEmbeddedCertificate(out X509Certificate2 certificate)
        {
            certificate = null;

            if (Token == null)
            {
                return false;
            }

            try
            {
                var x5c = ((JArray)Token.Header["x5c"]).Values<string>().First();

                certificate = new X509Certificate2(Convert.FromBase64String(x5c));

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
