using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace BackBank.Models.Settings
{
    public class TransportSecuritySettings
    {
        public TransportSecuritySettings(IWebHostEnvironment env)
            : this(env.EnvironmentName == Environments.Production) 
        {
        }

        private TransportSecuritySettings(bool enableAll)
        {
            SignatureRequred = enableAll;
            EncryptionRequired = enableAll;
        }

        public bool SignatureRequred { get; set; } = true;

        public bool EncryptionRequired { get; set; } = true;

        public bool RuntimeChecksRequired { get; set; } = false;

        public TimeSpan RuntimeChecksInterval { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan RuntimeChecksTimeout { get; set; } = TimeSpan.FromSeconds(75);
    }
}
