using BackBank.Internal;
using BackBank.Models.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BackBank.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        private readonly JwtSettings _jwtSettings;
        private readonly TransportSecuritySettings _transportOptions;
        private readonly JwtSecurityTokenHandler _jwtHandler;

        public JwtMiddleware(RequestDelegate next, JwtSettings jwtSettings, TransportSecuritySettings transportOptions, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _logger = logger;

            _jwtSettings = jwtSettings;
            _transportOptions = transportOptions;
            _jwtHandler = new JwtSecurityTokenHandler();
        }

        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;

            if(request.ContentLength > 0 && request.ContentType == null)
            {
                _logger.LogWarning("ContentType is null");

                context.Response.StatusCode = 462;
                return;
            }

            var signature = request.GetSignature();

            var canTransform = CanTransform(request);

            var needTransform = NeedTransform(request);

            if (needTransform)
            {
                if (canTransform)
                {
                    try
                    {
                        TransformRequest(request);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decode JWT body");

                        context.Response.StatusCode = 460;
                        return;
                    }
                }
                else if (_transportOptions.EncryptionRequired)
                {
                    _logger.LogWarning("Request not encrypted");

                    context.Response.StatusCode = 460;
                    return;
                }
            }

            var existingBody = context.Response.Body;   

            var newBody = new MemoryStream();
            try
            {
                // We set the response body to our stream so we can read after the chain of middlewares have been called
                context.Response.Body = newBody;

                await _next(context);
            }
            finally
            {
                var binaryBody = newBody.ToArray();

                if (canTransform && binaryBody.Length > 0 && signature?.Certificate != null)
                {
                    binaryBody = TransformResponse(binaryBody, signature.Certificate);
                    context.Response.ContentType = "application/jwt";
                }

                context.Response.WriteResponseSignature(binaryBody, signature, _jwtSettings.GetCertificate());

                // Set the stream back to the original
                context.Response.Body = existingBody;

                if (binaryBody.Length > 0)
                {
                    await context.Response.Body.WriteAsync(binaryBody, 0, binaryBody.Length);
                }

                newBody?.Dispose();
            }
        }

        protected virtual bool CanTransform(HttpRequest request)
        {
            if (request.ContentType == "application/jwt")
            {
                return true;
            }

            return false;
        }

        protected virtual bool NeedTransform(HttpRequest request)
        {
            if (request.ContentLength > 0 && !request.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        protected virtual void TransformRequest(HttpRequest request)
        {
            using (var reader = new StreamReader(request.Body))
            {
                var body = reader.ReadToEnd();
                _logger.LogInformation(body);

                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    RequireSignedTokens = false,
                    RequireExpirationTime = false,
                    TokenDecryptionKey = _jwtSettings.GetSecurityKey(),
                };


                _jwtHandler.ValidateToken(body, validationParams, out var securityToken);

                var jwtToken = (JwtSecurityToken)securityToken;

                var bytes = Base64UrlEncoder.DecodeBytes(jwtToken.InnerToken.RawPayload);

                var stream = new MemoryStream(bytes, 0, bytes.Length);

                request.Body = stream;
                request.ContentType = "application/json";
                request.ContentLength = bytes.Length;
            }
        }

        protected virtual byte[] TransformResponse(byte[] response, X509Certificate2 certificate)
        {
            var creds = new X509EncryptingCredentials(certificate, SecurityAlgorithms.RsaPKCS1, SecurityAlgorithms.Aes128CbcHmacSha256);

            var header = new JwtHeader(creds);
            var payload = new CustomPayload(Encoding.UTF8.GetString(response));

            var token = new JwtSecurityToken(header, payload);
            var responseBody = _jwtHandler.WriteToken(token);

            return Encoding.UTF8.GetBytes(responseBody);
        }

        private class CustomPayload : JwtPayload
        {
            private string _data;

            public CustomPayload(string data)
            {
                _data = data;
            }

            public override string SerializeToJson()
            {
                return _data;
            }
        }
    }
}
