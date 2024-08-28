using System;
namespace _2c2pTechAssign.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public required string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public required string CurrencyCode { get; set; }
        public DateTime TransactionDate { get; set; }
        public required string Status { get; set; }
    }
}
