using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Services.SmsSender
{
    public class SMSruResult
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("status_code")]
        public int StatusCode { get; set; }
        [JsonProperty("sms")]
        public Dictionary<string, SMSruPhone> SMS { get; set; }
        [JsonProperty("balance")]
        public double Balance { get; set; }
    }
}
