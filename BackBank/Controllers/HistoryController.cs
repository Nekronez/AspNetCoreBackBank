using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackBank.Internal;
using BackBank.Internal.Filters;
using BackBank.Models;
using BackBank.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BackBank.Controllers
{
    [ApiController]
    [SignatureFilter]
    [Produces("application/json")]
    public class HistoryController : ControllerBase
    {
        private AppDbContext _dbContext;

        public HistoryController(AppDbContext appDbContext)
        {
            _dbContext = appDbContext;
        }

        [Authorize]
        [HttpGet("/history")]
        public IActionResult GetHistory()
        {
            var history = _dbContext.historyOperations.Join(
                _dbContext.cards.Where(c => c.UserId == User.GetId()),
                h => h.CardId,
                c => c.Id,
                (h, c) => new {
                    CardName = c.Name,
                    h.OperationName,
                    h.OperationStatus,
                    h.Amount,
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
            Card card = await _dbContext.cards.Where(c => c.Id == cardId && c.UserId == User.GetId()).FirstOrDefaultAsync();

            if (card == null)
                return NotFound(new { Messages = new[] { "Not found." } });

            var history = _dbContext.historyOperations.Join(
                _dbContext.cards.Where(c => c.UserId == User.GetId() && c.Id == cardId),
                h => h.CardId,
                c => c.Id,
                (h, c) => new {
                    CardName = c.Name,
                    h.OperationName,
                    h.OperationStatus,
                    h.Amount,
                    h.CreatedAt
                });

            if (history.ToList().Count == 0)
                return NoContent();

            return Ok(history);
        }
    }
}