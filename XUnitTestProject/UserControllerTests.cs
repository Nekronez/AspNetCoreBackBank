using BackBank.Controllers;
using BackBank.Models;
using BackBank.Models.Incoming;
using BackBank.Models.Settings;
using BackBank.Services.SmsSender;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using Xunit;

namespace XUnitTestProject
{
    public class UserControllerTests
    {
        private readonly ISmsSender smsSender;
        private readonly AppDbContext dbContext;
        private readonly IOptions<TokenSettings> tokenOptions;
        private readonly IOptions<SMSSettings> smsSettings;
        private readonly JwtSettings jwtSettings;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            var services = new ServiceCollection();

            services.AddHttpClient<SMSruSender>();
            services.AddTransient<ISmsSender, SMSruSender>();
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql("Server=srv-per-websql; Username=postgres; Password=Techno2018; Database=backBankDB;"));
            
            var serviceProvider = services.BuildServiceProvider();

            smsSender = serviceProvider.GetService<ISmsSender>();
            dbContext = serviceProvider.GetService<AppDbContext>();
            tokenOptions = Options.Create(new TokenSettings() { Audience = "BankApi", Issuer = "BankApi", AuthLifetime = 5000 });
            smsSettings = Options.Create(new SMSSettings() { ApiId = "FA32DFE1-A351-A74B-037A-B5BD09E04AA4", OTPSecretKey = "958AOZhnNVTZOwEKrYW3UHXxQG9URr1" });
            jwtSettings = new JwtSettings() { Certificate = new CertificateSettings() { Password="1234", Path= @"D:\Users\alexander.babushkin\source\repos\BackBank\BackBank\Secrets\bank.pfx" } };

            _controller = new UserController(smsSender, dbContext, tokenOptions, smsSettings, jwtSettings);
        }

        [Fact]
        public void RegistrationOk()
        {
            // Act
            var result = _controller.Registration(new RegistrModel() { PAN= "5555555555554444", Phone= "79220000001" });
            var okResult = result as OkObjectResult;

            // Assert
            Assert.NotNull(okResult);
        }

        [Fact]
        public async void RegistrationInvalidModel()
        {
            // Act
            var result = _controller.Registration(new RegistrModel() { PAN = "555555", Phone = "79220000" });
            var okResult = result as ObjectResult;

            // Assert
            Assert.NotNull(okResult);
        }
    }
}
