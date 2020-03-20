using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackBank.Models.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackBank.Controllers
{
    public class InfoController : ControllerBase
    {
        private readonly JwtSettings _jwtOptions;

        public InfoController(JwtSettings jwtOptions)
        {
            _jwtOptions = jwtOptions;
        }

        [HttpGet("jwt")]
        [Produces("application/x-x509-user-cert")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetJwtCertificate()
        {
            var certificate = _jwtOptions.GetCertificate();

            var builder = new StringBuilder();

            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(Convert.ToBase64String(certificate.RawData, Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

            return Content(builder.ToString(), "application/x-x509-user-cert");
        }
    }
}