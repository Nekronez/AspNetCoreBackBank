using RestEase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Services.SmsSender
{
    public interface ISMSruApi
    {
        [Get("sms/send?json=1")]
        Task<SMSruResult> SendSmsAsync([Query("api_id")] string ApiId,
                                           [Query("to")] string Phone,
                                           [Query("msg")] string Message);
    }
}
