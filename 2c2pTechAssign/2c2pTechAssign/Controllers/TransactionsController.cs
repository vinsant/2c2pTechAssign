using System;
using _2c2pTechAssign.Models;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Xml.Linq;
using _2c2pTechAssign.Contexts;
using _2c2pTechAssign.Interfaces;

namespace _2c2pTechAssign.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0 || file.Length > 1 * 1024 * 1024)
                return BadRequest("Invalid file size");

            try
            {
                var (transactions, validationLogs) = await _transactionService.ProcessFileAsync(file);

                if (validationLogs.Count > 0)
                    return BadRequest(validationLogs);

                await _transactionService.SaveTransactionsAsync(transactions);
                return Ok();
            }
            catch (FormatException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] string currency, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string status)
        {
            var transactions = await _transactionService.GetTransactionsAsync(currency, startDate, endDate, status);
            return Ok(transactions.Select(t => new
            {
                t.TransactionId,
                Payment = $"{t.Amount} {t.CurrencyCode}",
                Status = t.Status
            }));
        }
    }
}

