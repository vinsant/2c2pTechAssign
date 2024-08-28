using System;
using _2c2pTechAssign.Models;

namespace _2c2pTechAssign.Interfaces
{
    public interface ITransactionService
    {
        Task<(List<Transaction>, List<ValidationLog>)> ProcessFileAsync(IFormFile file);
        Task SaveTransactionsAsync(List<Transaction> transactions);
        Task<List<Transaction>> GetTransactionsAsync(string currency, DateTime? startDate, DateTime? endDate, string status);
    }

}

