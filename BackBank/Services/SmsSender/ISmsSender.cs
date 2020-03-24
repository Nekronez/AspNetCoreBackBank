using RestEase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BackBank.Services.SmsSender
{
    public interface ISmsSender
    {
        Task<HttpStatusCode> SendSmsAsync(string phone, string message);
    }
}
