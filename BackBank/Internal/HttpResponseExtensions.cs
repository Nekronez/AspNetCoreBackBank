using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BackBank.Internal
{
    internal static class HttpResponseExtensions
    {
        public static void WriteResponseSignature(this HttpResponse response, byte[] responseBody, RequestSignature signature, X509Certificate2 certificate)
        {
            var context = response.HttpContext;
            var request = context.Request;
            var logger = context.RequestServices.GetRequiredService<ILogger<HttpResponse>>();

            var fields = new Dictionary<string, string>();

            fields.Add("method", request.Method);

            var body = Encoding.UTF8.GetString(responseBody);

            if (!string.IsNullOrEmpty(body))
            {
                fields.Add("body", ComputeHash(body));
            }

            var responseSignature = IssueSignature(fields, certificate);

            logger.LogTrace($"X-Response-Signature: {responseSignature}");

            response.Headers.Add("X-Response-Signature", responseSignature);
        }

        private static string ComputeHash(string input)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return string.Concat(hash.Select(b => b.ToString("X2")));
            }
        }

        public static string IssueSignature(Dictionary<string, string> fields, X509Certificate2 certificate)
        {
            var claims = fields.Select(x => new Claim(x.Key, x.Value));

            var creds = new SigningCredentials(new X509SecurityKey(certificate), SecurityAlgorithms.RsaSha256);

            var token = new JwtSecurityToken(
                issuer: "BankApi",
                notBefore: DateTime.UtcNow,
                claims: claims,
                signingCredentials: creds);
            token.Header.Remove("kid");

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
