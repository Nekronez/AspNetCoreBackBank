using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BackBank.Models.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestEase;

namespace BackBank.Services.SmsSender
{
    public class SMSruSender : ISmsSender
    {
        private IHttpClientFactory _clientFactory;
        private SMSSettings _SMSSettings;

        public SMSruSender(IHttpClientFactory clientFactory, IOptions<SMSSettings> SMSSettings)
        {
            _clientFactory = clientFactory;
            _SMSSettings = SMSSettings.Value;
        }

        public async Task<HttpStatusCode> SendSmsAsync(string phone, string message)
        {
            var apiId = _SMSSettings.ApiId;

            ISMSruApi smsApi = RestClient.For<ISMSruApi>("https://sms.ru");
            var result = smsApi.SendSmsAsync(apiId, phone, message).Result;

            if (result.Status == "OK")
            {
                return HttpStatusCode.OK;
            }
            else
            {
                return HttpStatusCode.BadRequest;
            }
        }
    }
}
