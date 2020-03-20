using BackBank.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.AspNetCore.Http
{
    internal static class HttpRequestExtensions
    {
        public static RequestSignature GetSignature(this HttpRequest request)
        {
            var context = request.HttpContext;
            var logger = context.RequestServices.GetRequiredService<ILogger<RequestSignature>>();

            // First we check if we have already saved a signature on the context
            if (context.Items.TryGetValue(typeof(RequestSignature), out var contextItem))
            {
                if (contextItem is RequestSignature contextSignature)
                {
                    return contextSignature;
                }
            }

            // Try to retrieve from header
            if (!request.Headers.TryGetValue("X-Request-Signature", out var headerValues))
            {
                return null;
            }

            var jws = headerValues.ToString();

            // Verify header is set
            if (string.IsNullOrWhiteSpace(jws))
            {
                return null;
            }

            logger.LogTrace($"X-Request-Signature: {jws}");

            RequestSignature signature;
            try
            {
                var token = new JwtSecurityToken(jws);

                signature = new RequestSignature(request, token);
            }
            catch (Exception e)
            {
                signature = new RequestSignature();
            }

            // Save to context
            context.Items[typeof(RequestSignature)] = signature;

            return signature;
        }
    }
}
