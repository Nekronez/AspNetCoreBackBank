using BackBank.Models;
using BackBank.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Internal
{
    public class TokenClaimsHandler : AuthorizationHandler<TokenClaimsRequirement>
    {
        private AppDbContext _appDbContext;
        private HttpContext _httpContext;
        private TokenSettings _tokenSettings;
        private IWebHostEnvironment _env;

        public TokenClaimsHandler(AppDbContext appDbContext, IHttpContextAccessor httpContextAccessor, IOptions<TokenSettings> options, IWebHostEnvironment env)
        {
            _appDbContext = appDbContext;
            _httpContext = httpContextAccessor.HttpContext;
            _tokenSettings = options.Value;
            _env = env;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       TokenClaimsRequirement requirement)
        {
            if (_httpContext.Response.StatusCode == StatusCodes.Status403Forbidden)
                return Task.CompletedTask;

            _httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader);
            if (authHeader.Count == 0)
            {
                EditResponse(401, "Authentication is required to access the requested resource.");
                return Task.CompletedTask;
            }

            var userId = context.User.Claims.Where(c => c.Type == "idUser").FirstOrDefault();
            if (userId == null)
            {
                EditResponse(403, "Claim idUser is required.");
                return Task.CompletedTask;
            }

            var tokenType = context.User.Claims.Where(c => c.Type == "type").FirstOrDefault();
            if (tokenType == null)
            {
                EditResponse(403, "Claim type is required.");
                return Task.CompletedTask;
            }

            if (requirement.IsAuthToken)
            {
                if (tokenType.Value.ToString() != "Auth")
                {
                    EditResponse(403, "Wrong type of token.");
                    return Task.CompletedTask;
                }

                string token = authHeader.ToString().Remove(0, 7);

                AuthSession authSession = _appDbContext.authSessions.Where(a => a.UserId == int.Parse(userId.Value) && a.Token == token.ToString()).ToList().LastOrDefault();

                if (authSession == null || authSession.ExpirationTime < DateTime.Now)
                {
                    EditResponse(403, "Token is expired.");
                    return Task.CompletedTask;
                }

                authSession.ExpirationTime = DateTime.UtcNow.Add(TimeSpan.FromMinutes(_tokenSettings.AuthLifetime));
                _appDbContext.Update(authSession);
                _appDbContext.SaveChanges();
            }
            else
            {
                if (tokenType.Value.ToString() != "Session")
                {
                    EditResponse(403, "Wrong type of token.");
                    return Task.CompletedTask;
                }
            }
            
            context.Succeed(requirement);

            return Task.CompletedTask;
        }

        private void EditResponse(int statusCode, string message)
        {
            _httpContext.Response.StatusCode = statusCode;
            if(_env.EnvironmentName == "Development")
            {
                _httpContext.Response.ContentType = "application/json; charset=utf-8";
                var result = JsonConvert.SerializeObject(new { messages = new[] { message } });
                _httpContext.Response.WriteAsync(result);
            }
        }
    }
}
