using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BackBank.Internal;
using BackBank.Middleware;
using BackBank.Models;
using BackBank.Models.Settings;
using BackBank.Services.SmsSender;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace BackBank
{
    public class Startup
    {
        public IConfiguration _config { get; }
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _env = env;
            _config = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.KnownProxies.Add(IPAddress.Parse("172.27.3.3"));
            });

            var connection = _config.GetConnectionString("DefaultConnection");
            var tokenSection = _config.GetSection("TokenSettings");
            var tokenSettings = tokenSection.Get<TokenSettings>();
            var jwtSettings = _config.GetSection("Jwt")
                                     .Get<JwtSettings>();

            services.Configure<TokenSettings>(tokenSection);
            services.Configure<SMSSettings>(_config.GetSection("SMSSettings"));


            services.AddTransient<ISmsSender, SMSruSender>();
            services.AddSingleton(jwtSettings);
            services.AddSingleton(new TransportSecuritySettings(_env));
            services.AddSingleton<TraceIdentifierProviderExtension>();

            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                if (_env.IsDevelopment())
                {
                    options.Events = new JwtBearerEvents()
                    {
                        OnAuthenticationFailed = context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json; charset=utf-8";
                            string message = "";
                            if (context.Exception is SecurityTokenInvalidAudienceException)
                                message = "Audience of a token was not valid.";
                            if (context.Exception is SecurityTokenInvalidIssuerException)
                                message = "Issuer of a token was not valid.";
                            if (context.Exception is SecurityTokenInvalidSignatureException)
                                message = "Signature of a token was not valid.";
                            if (message == "")
                                message = "Invalid token.";

                            var result = JsonConvert.SerializeObject(new { Messages = new[] { message } });
                            return context.Response.WriteAsync(result);
                        },
                    };
                }
            
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = tokenSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = tokenSettings.Audience,
                    ValidateLifetime = false,
                    IssuerSigningKey = jwtSettings.GetSecurityKey(),
                    ValidateIssuerSigningKey = true,
                };
            })
            .AddJwtBearer("Session", options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = tokenSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = tokenSettings.Audience,
                    ValidateLifetime = false,
                    IssuerSigningKey = jwtSettings.GetSecurityKey(),
                    ValidateIssuerSigningKey = true,
                };
            });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    //.RequireClaim("type","Auth")
                    //.RequireClaim("idUser")
                    .AddRequirements(new TokenClaimsRequirement(true))
                    .Build();
                options.AddPolicy("SessionPolicy", new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    //.RequireClaim("type", "Session")
                    //.RequireClaim("idUser")
                    .AddRequirements(new TokenClaimsRequirement(false))
                    .AddAuthenticationSchemes("Session")
                    .Build());
            });
            services.AddHttpContextAccessor();
            services.AddTransient<IAuthorizationHandler, TokenClaimsHandler>();

            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.AddHttpClient();
            services.AddControllers().ConfigureApiBehaviorOptions(options => {
                options.SuppressMapClientErrors = true;// Remove status and traceId from error response
            });

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    List<string> result = new List<string>();

                    foreach (var modelStateKey in actionContext.ModelState.Keys)
                    {
                        var value = actionContext.ModelState[modelStateKey];
                        foreach (var error in value.Errors)
                        {
                            result.Add(error.ErrorMessage);
                        }
                    }

                    return new BadRequestObjectResult(new { Messages = result });
                };
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseMiddleware<HttpTraceHeaderMiddleware>();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<JwtMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                
                endpoints.MapControllers();
            });
        }
    }
}
