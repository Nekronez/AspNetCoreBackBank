using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Security.Principal;
using BackBank.Models;

namespace BackBank.Internal
{
    public static class PrincipalExtensio
    {
        public static int GetId(this ClaimsPrincipal claimsPrincipal)
        {
            return int.Parse(claimsPrincipal.Claims.Where(c => c.Type == "idUser").FirstOrDefault().Value);
        }
    }
}
