using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Services.SmsSender
{
    public class SMSruPhone
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("status_code")]
        public int StatusCode { get; set; }
        [JsonProperty("status_text")]
        public string StatusText { get; set; }
        [JsonProperty("coast")]
        public double Cost { get; set; }
    }
}
