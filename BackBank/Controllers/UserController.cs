using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BackBank.Models;
using BackBank.Models.Incoming;
using BackBank.Models.Settings;
using BackBank.Services.SmsSender;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using BackBank.Internal.Filters;
using Newtonsoft.Json;
using OtpNet;
using Npgsql;

namespace BackBank.Controllers
{
    [ApiController]
    [SignatureFilter]
    [Produces("application/json")]
    public class UserController : ControllerBase
    {
        enum TypeToken
        {
            Session,
            Аuthorization
        }

        private ISmsSender _smsSender;
        private AppDbContext _dbContext;
        private TokenSettings _tokenSettings;
        private readonly JwtSettings _jwtSettings;
        private readonly SMSSettings _SMSSettings;

        public UserController(ISmsSender smsSender, 
                              AppDbContext appDbContext, 
                              IOptions<TokenSettings> tokenOptions,
                              IOptions<SMSSettings> smsOptions,
                              JwtSettings jwtSettings)
        {
            _smsSender = smsSender;
            _dbContext = appDbContext;
            _tokenSettings = tokenOptions.Value;
            _jwtSettings = jwtSettings;
            _SMSSettings = smsOptions.Value;
        }

        [HttpPost("/user/register")]
        public async Task<IActionResult> Registration([FromBody] RegistrModel model)
        {
            // Checking user existence
            User user = _dbContext.users.Where(u => u.Phone == model.Phone).FirstOrDefault();
            if (user == null)
                return BadRequest(new { Messages = new[] { "Invalid phone or card number." } });

            // Verify PAN
            Card card = _dbContext.cards.Where(c => c.PAN == model.PAN).FirstOrDefault();
            if (!CheckCardNumber(model.PAN) || card == null || card.UserId != user.Id)
                return BadRequest(new { Messages = new[] { "Invalid phone or card number." } });

            // Check if SMS was sent recently to this user
            SmsSession smsSession = _dbContext.smsSessions.Where(s => s.UserId == user.Id)
                                                          .OrderBy(s => s.Id)
                                                          .Include(s => s.User)
                                                          .LastOrDefault();
            if (smsSession != null)
                if (Math.Abs((smsSession.CreatedAt - DateTime.UtcNow).Minutes) < 1)
                    return BadRequest(new { Messages = new[] { "SMS code has already been sent to you. To send a new one, wait a minute from the moment of sending the past." } });

            // Generate OTP
            long hotpCounter = _dbContext.GetHotpCounter();
            Hotp hotp = new Hotp(Encoding.ASCII.GetBytes(_SMSSettings.OTPSecretKey), mode: OtpHashMode.Sha256, hotpSize: 6);
            string hotpCode = hotp.ComputeHOTP(1/*hotpCounter*/);

            // Send SMS
            // HttpStatusCode sendingResult = await _smsSender.SendSmsAsync(model.Phone, hotpCode);
            // if (sendingResult != HttpStatusCode.OK)
            //    return StatusCode(500, new { Message = "Error sending SMS code." });

            // Save SmsSession
            smsSession = new SmsSession() { UserId = user.Id, CodeHotpCounter = 1/*hotpCounter*/, CreatedAt = DateTime.UtcNow };
            _dbContext.smsSessions.Add(smsSession);
            await _dbContext.SaveChangesAsync();

            return StatusCode(201, new
            {
                Message = "An SMS with a verification code has been sent to your number.",
                SessionToken = GetToken(TypeToken.Session, user.Id)
            });
        }

        [HttpPost("/verify/phone")]
        [Authorize("SessionPolicy")]
        public async Task<IActionResult> VerifyPhone([FromBody] SmsCodeModel model)
        {
            int userId = int.Parse(User.Claims.Where(c => c.Type == "idUser").FirstOrDefault().Value);

            SmsSession smsSession = _dbContext.smsSessions.Where(s => s.UserId == userId).OrderBy(s => s.Id).LastOrDefault();
            if (smsSession == null)
                return NotFound(new { Messages = new[] { "SMS code was not sent to the phone number." } });
            if ((DateTime.UtcNow - smsSession.CreatedAt) > TimeSpan.FromMinutes(2))
                return BadRequest(new { Messages = new[] { "The previously sent code is out of date. Send a request to resend SMS." } });

            Hotp hotp = new Hotp(Encoding.ASCII.GetBytes(_SMSSettings.OTPSecretKey), mode: OtpHashMode.Sha256, hotpSize: 6);
            smsSession.Attempts++;
            if (!hotp.VerifyHotp(model.Code, smsSession.CodeHotpCounter))
            {
                if (smsSession.Attempts >= 5)
                    return BadRequest(new { Messages = new[] { "The number of incorrect attempts to enter SMS code is exceeded. Send a request to resend SMS." } });

                await _dbContext.SaveChangesAsync();
                return BadRequest(new { Messages = new[] { "Incorrect SMS code entered." } });
            }
            smsSession.Checked = true;

            await _dbContext.SaveChangesAsync();

            return Ok(new { AuthToken = GetToken(TypeToken.Аuthorization, userId) });
        }

        [HttpPost("/send/sms")]
        [Authorize("SessionPolicy")]
        public async Task<IActionResult> SendNewSms()
        {
            int userId = int.Parse(User.Claims.Where(c => c.Type == "idUser").FirstOrDefault().Value);

            SmsSession smsSession = _dbContext.smsSessions.Where(s => s.UserId == userId)
                                                          .OrderBy(s => s.Id)
                                                          .Include(s => s.User)
                                                          .LastOrDefault();
            if (smsSession == null)
                return BadRequest(new { Messages = new[] { "Resending SMS is only available after registration." } });
            if (Math.Abs((smsSession.CreatedAt - DateTime.UtcNow).Minutes) < 1)
                return BadRequest(new { Messages = new[] { "SMS code has already been sent to you. To send a new one, wait a minute from the moment of sending the past." } });

            // Generate OTP
            long hotpCounter = _dbContext.GetHotpCounter();
            Hotp hotp = new Hotp(Encoding.ASCII.GetBytes(_SMSSettings.OTPSecretKey), mode: OtpHashMode.Sha256, hotpSize: 6);
            string hotpCode = hotp.ComputeHOTP(1/*hotpCounter*/);

            //HttpStatusCode sendingResult = await _smsSender.SendSmsAsync(user.Phone, hotpCode);
            //if (sendingResult != HttpStatusCode.OK)
            //    return StatusCode(500, new { Message = "Error sending SMS code." });

            SmsSession newSmsSession = new SmsSession() { UserId = smsSession.User.Id, CodeHotpCounter= 1/*hotpCode*/, CreatedAt = DateTime.UtcNow };
            _dbContext.smsSessions.Add(newSmsSession);
            await _dbContext.SaveChangesAsync();

            return StatusCode(201, new { Message = "SMS with a verification code has been sent to your number." });
        }

        [HttpPost("/auth")]
        [Authorize("SessionPolicy")]
        public async Task<IActionResult> Autauthentication([FromBody] AuthModel model)
        {
            int userId = int.Parse(User.Claims.Where(c => c.Type == "idUser").FirstOrDefault().Value);
            User user = await _dbContext.users.FindAsync(userId);

            if(user == null || user.Passcode != GetHashSHA256(model.Passcode))
                return BadRequest(new { Messages = new[] { "Invalid passcode." } });

            return Ok(new { AuthToken = GetToken(TypeToken.Аuthorization, user.Id) });
        }

        [Authorize]
        [HttpPost("/user/passcode")]
        public async Task<IActionResult> CreatePasscode([FromBody] PasscodeModel model)
        {
            int userId = int.Parse(User.Claims.Where(c => c.Type == "idUser").FirstOrDefault().Value);

            User user = _dbContext.users.Find(userId);
            user.Passcode = GetHashSHA256(model.Passcode);
            await _dbContext.SaveChangesAsync();

            return StatusCode(201, new { Message = "Passcode created successfully." });
        }

        [Authorize]
        [HttpGet("/card")]
        public async Task<IActionResult> GetCards()
        {
            int userId = int.Parse(User.Claims.Where(c => c.Type == "idUser").FirstOrDefault().Value);

            List<Card> cards = await _dbContext.cards.Where(c => c.UserId == userId).ToListAsync();

            return Ok(cards);
        }

        [Authorize]
        [HttpGet("/card/{id}")]
        public async Task<IActionResult> GetCardInfo(int id)
        {
            int userId = int.Parse(User.Claims.Where(c => c.Type == "idUser").FirstOrDefault().Value);

            Card card = await _dbContext.cards.Where(c => c.Id == id).FirstOrDefaultAsync();

            if (card == null)
                return NotFound(new { Messages = new[] { "Not found." } });
            if (card.UserId != userId)
            {
                HttpContext.Response.ContentType = "application/json; charset=utf-8";
                var result = JsonConvert.SerializeObject(new { Messages = new[] { "Access denied." } });
                await HttpContext.Response.WriteAsync(result);
                return Forbid();
            }

            return Ok(card);
        }

        [Authorize]
        [HttpGet("/history")]
        public IActionResult GetHistory()
        {
            int userId = int.Parse(User.Claims.Where(c => c.Type == "idUser").FirstOrDefault().Value);

            var history = _dbContext.historyOperations.Join(
                _dbContext.cards.Where(c => c.UserId == userId),
                h => h.CardId, 
                c => c.Id, 
                (h, c) => new { 
                    CardName = c.Name,
                    h.OperationName,
                    h.OperationStatus,
                    h.OperationSumma,
                    h.CreatedAt
                });

            if (history.ToList().Count == 0)
                return NoContent();

            return Ok(history);
        }

        [Authorize]
        [HttpGet("/history/{cardId}")]
        public async Task<IActionResult> GetHistoryCard(int cardId)
        {
            int userId = int.Parse(User.Claims.Where(c => c.Type == "idUser").FirstOrDefault().Value);

            Card card = await _dbContext.cards.Where(c => c.Id == cardId).FirstOrDefaultAsync();

            if (card == null)
                return NotFound( new { Messages = new[] { "Not found." } });

            if (card.UserId != userId)
            {
                HttpContext.Response.ContentType = "application/json; charset=utf-8";
                var result = JsonConvert.SerializeObject(new { Messages = new[] { "Access denied." } });
                await HttpContext.Response.WriteAsync(result);
                return Forbid();
            }
                
                

            var history = _dbContext.historyOperations.Join(
                _dbContext.cards.Where(c => c.UserId == userId && c.Id == cardId),
                h => h.CardId,
                c => c.Id,
                (h, c) => new {
                    CardName = c.Name,
                    h.OperationName,
                    h.OperationStatus,
                    h.OperationSumma,
                    h.CreatedAt
                });

            if (history.ToList().Count == 0)
                return NoContent();

            return Ok(history);
        }

        private string GetToken(TypeToken type, int userId)
        {
            DateTime now = DateTime.UtcNow;
            DateTime? expire;
            string key;
            User user = _dbContext.users.Find(userId);
            AuthSession authSession = null;
            var claims = new List<Claim>
            {
                new Claim("idUser", Convert.ToString(userId)),
                new Claim("nbf", Convert.ToString(DateTimeOffset.Now.ToUnixTimeSeconds()))
            };

            if (type == TypeToken.Аuthorization)
            {
                //expire = now.Add(TimeSpan.FromMinutes(_tokenSettings.AuthLifetime));
                claims.Add(new Claim("type", "Auth"));
                claims.Add(new Claim("type", "Auth"));
                authSession = new AuthSession() {
                    ExpirationTime = now.Add(TimeSpan.FromMinutes(_tokenSettings.AuthLifetime)),
                    UserId = userId,
                    CreatedAt = now
                };
            }
            else
            {
                claims.Add(new Claim("type", "Session"));
            }

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(
                claims,
                "Token"
            );

            var creds = new SigningCredentials(new X509SecurityKey(_jwtSettings.GetCertificate()), SecurityAlgorithms.RsaSha256);

            var jwt = new JwtSecurityToken(
                    issuer: _tokenSettings.Issuer,
                    audience: _tokenSettings.Audience,
                    claims: claimsIdentity.Claims,
                    signingCredentials: creds
                    );
            jwt.Header.Remove("kid");
            string token = new JwtSecurityTokenHandler().WriteToken(jwt);

            if (type == TypeToken.Аuthorization)
            {
                authSession.Token = token;
                _dbContext.authSessions.Add(authSession);
                _dbContext.SaveChanges();
            }

            return token;
        }

        private string GetHashSHA256(string source)
        {
            SHA256 sha256Hash = SHA256.Create();
            byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
            byte[] hashBytes = sha256Hash.ComputeHash(sourceBytes);
            string hash = BitConverter.ToString(hashBytes).Replace("-", String.Empty);
            return hash;
        }

        private static Dictionary<string, Regex> cards = new Dictionary<string, Regex>()
        {
            ["American Express"] = new Regex(@"\A3[47][0-9]{13}\z"),
            ["MasterCard"] = new Regex(@"\A5[1-5][0-9]{14}\z"),
            ["Visa"] = new Regex(@"\A4[0-9]{12}(?:[0-9]{3})?\z")
        };

        public static bool CheckCardNumber(string str)
        {
            int sum = 0;
            string card;
            if (!Int64.TryParse(str, out long result))
                return false;
            if ((str.StartsWith("34") || str.StartsWith("37")) && (str.Length == 15))
                card = "American Express";
            else if ((str.StartsWith("51")) || (str.StartsWith("52")) ||
               (str.StartsWith("53")) || (str.StartsWith("54")) ||
               (str.StartsWith("55")) && (str.Length == 16))
                card = "MasterCard";
            else if ((str.StartsWith("4")) && ((str.Length == 13) || (str.Length == 16)))
                card = "Visa";
            else
                return false;

            int len = str.Length;
            for (int i = 0; i < len; i++)
            {
                int add = (str[i] - '0') * (2 - (i + len) % 2);
                add -= add > 9 ? 9 : 0;
                sum += add;
            }

            if (sum % 10 != 0)
                return false;
            return true;
        }
    }
}