using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Internal
{
    public class TokenClaimsRequirement : IAuthorizationRequirement
    {
        public bool IsAuthToken { get; set; }

        public TokenClaimsRequirement(bool isAuthToken)
        {
            IsAuthToken = isAuthToken;
        }
    }
}
