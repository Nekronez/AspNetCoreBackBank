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
            // НУЖНО ВЫНЕСТИ api_id В appsettings.json
            var api_id = _SMSSettings.ApiId;
            var uri = $"https://sms.ru/sms/send?api_id={api_id}&to={phone}&msg={message}&json=1";
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("User-Agent", "backBank");

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
            var jsonString = JObject.Parse(responseString);

            if(jsonString["sms"][phone]["status"].ToString() == "OK")
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
