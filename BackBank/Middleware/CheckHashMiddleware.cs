using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace BackBank.Middleware
{
    public class CheckHashMiddleware
    {
        private readonly RequestDelegate _next;

        public CheckHashMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string requestHash = context.Request.Headers["Hash"];
            if(requestHash == null)
            {
                var result = new { Message = "Missing header Hash." };
                var json = JsonConvert.SerializeObject(result);
                context.Response.StatusCode = 401;

                await context.Response.WriteAsync(json);
                return;
            }
            else
            {
                context.Request.EnableBuffering();
                using (var reader = new StreamReader(
                    context.Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true))
                {
                    var body = await reader.ReadToEndAsync();

                    byte[]  bodyByteArr;
                    //SHA1 sha1 = new SHA1CryptoServiceProvider();
                    //bodyByteArr = sha1.ComputeHash(Encoding.Default.GetBytes("test"));
                    
                    SHA256 sha256 = SHA256.Create();
                    bodyByteArr = sha256.ComputeHash(Encoding.Default.GetBytes("test"));


                    // Sign
                    byte[] hashValue = bodyByteArr;
                    byte[] signedHashValue;
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                    RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
                    rsaFormatter.SetHashAlgorithm("SHA256");
                    signedHashValue = rsaFormatter.CreateSignature(hashValue);
                    RSAParameters parametrs = rsa.ExportParameters(false);
                    var modulus = Convert.ToBase64String(parametrs.Modulus);// для хедеров
                    var exponent = Convert.ToBase64String(parametrs.Exponent);

                    // Verify
                    //RSAParameters rsaKeyInfo = new RSAParameters();
                    //rsaKeyInfo.Modulus = Convert.FromBase64String(context.Request.Headers["Modulus"]);
                    //rsaKeyInfo.Exponent = Convert.FromBase64String(context.Request.Headers["Exponent"]);

                    rsa = new RSACryptoServiceProvider();
                    rsa.ImportParameters(parametrs/*rsaKeyInfo*/);
                    RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                    rsaDeformatter.SetHashAlgorithm("SHA256");
                    //if (rsaDeformatter.VerifySignature(hashValue, signedHashValue))


                    //if (bodyHash.ToLower() != requestHash.ToLower())
                    //{
                    //    var result = new { Message = "Invalid Hash." };
                    //    var json = JsonConvert.SerializeObject(result);
                    //    context.Response.StatusCode = 401;

                    //    await context.Response.WriteAsync(json);
                    //    return;
                    //}
                }

                context.Request.Body.Position = 0;
                await _next(context);
            }
        }
    }
}
