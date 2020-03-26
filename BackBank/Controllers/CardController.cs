using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackBank.Internal;
using BackBank.Internal.Filters;
using BackBank.Models;
using BackBank.Models.Settings;
using BackBank.Services.SmsSender;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OtpNet;

namespace BackBank.Controllers
{
    [ApiController]
    [SignatureFilter]
    [Produces("application/json")]
    public class CardController : ControllerBase
    {
        private AppDbContext _dbContext;

        public CardController(AppDbContext appDbContext)
        {
            _dbContext = appDbContext;
        }

        [Authorize]
        [HttpGet("/card")]
        public async Task<IActionResult> GetCards()
        {
            List<Card> cards = await _dbContext.cards.Where(c => c.UserId == User.GetId()).ToListAsync();

            return Ok(cards);
        }

        [Authorize]
        [HttpGet("/card/{id}")]
        public async Task<IActionResult> GetCardInfo(int id)
        {
            Card card = await _dbContext.cards.Where(c => c.Id == id && c.UserId == User.GetId()).FirstOrDefaultAsync();

            if (card == null)
                return NotFound(new { Messages = new[] { "Not found." } });

            return Ok(card);
        }
    }
}