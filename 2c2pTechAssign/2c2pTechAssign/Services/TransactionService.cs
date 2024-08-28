using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using _2c2pTechAssign.Contexts;
using _2c2pTechAssign.Interfaces;
using _2c2pTechAssign.Models;
using CsvHelper;
using Microsoft.EntityFrameworkCore;

namespace _2c2pTechAssign.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly DBContext _context;

        public TransactionService(DBContext context)
        {
            _context = context;
        }

        public async Task<(List<Transaction>, List<ValidationLog>)> ProcessFileAsync(IFormFile file)
        {
            string extension = Path.GetExtension(file.FileName).ToLower();
            using (var stream = file.OpenReadStream())
            {
                switch (extension)
                {
                    case ".csv":
                        return await ProcessCsvAsync(stream);
                    case ".xml":
                        return await ProcessXmlAsync(stream);
                    default:
                        throw new FormatException("Unknown file format");
                }
            }
        }

        private async Task<(List<Transaction>, List<ValidationLog>)> ProcessCsvAsync(Stream stream)
        {
            List < Transaction > transactions = new();
            List<ValidationLog> validationLogs = new();

            using (StreamReader reader = new StreamReader(stream))
            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {

                var records = csv.GetRecords<dynamic>();

                foreach (var record in records)
                {
                    try
                    {
                        Transaction transaction = new Transaction
                        {
                            TransactionId = record.TransactionId,
                            Amount = decimal.Parse(record.Amount),
                            CurrencyCode = record.CurrencyCode,
                            TransactionDate = DateTime.ParseExact(record.TransactionDate, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                            Status = MapCsvStatus(record.Status)
                        };

                        if (!ValidateTransaction(transaction, out string? errorMessage))
                        {
                            validationLogs.Add(new ValidationLog
                            {
                                TransactionId = transaction.TransactionId,
                                ErrorMessage = errorMessage
                            });
                        }
                        else
                        {
                            transactions.Add(transaction);
                        }
                    }
                    catch (Exception ex)
                    {
                        validationLogs.Add(new ValidationLog
                        {
                            TransactionId = record.TransactionId,
                            ErrorMessage = ex.Message
                        });
                    }
                }
            }

            return (transactions, validationLogs);
        }

        private async Task<(List<Transaction>, List<ValidationLog>)> ProcessXmlAsync(Stream stream)
        {
            var transactions = new List<Transaction>();
            var validationLogs = new List<ValidationLog>();

            var xdoc = XDocument.Load(stream);
            var elements = xdoc.Descendants("Transaction");

            foreach (var element in elements)
            {
                try
                {
                    var transaction = new Transaction
                    {
                        TransactionId = element.Attribute("id").Value,
                        Amount = decimal.Parse(element.Element("PaymentDetails").Element("Amount").Value),
                        CurrencyCode = element.Element("PaymentDetails").Element("CurrencyCode").Value,
                        TransactionDate = DateTime.Parse(element.Element("TransactionDate").Value),
                        Status = MapXmlStatus(element.Element("Status").Value)
                    };

                    if (!ValidateTransaction(transaction, out var errorMessage))
                    {
                        validationLogs.Add(new ValidationLog
                        {
                            TransactionId = transaction.TransactionId,
                            ErrorMessage = errorMessage
                        });
                    }
                    else
                    {
                        transactions.Add(transaction);
                    }
                }
                catch (Exception ex)
                {
                    validationLogs.Add(new ValidationLog
                    {
                        TransactionId = element.Attribute("id").Value,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return (transactions, validationLogs);
        }

        private string MapCsvStatus(string status)
        {
            return status switch
            {
                "Approved" => "A",
                "Failed" => "R",
                "Finished" => "D",
                _ => throw new ArgumentException("Unknown status")
            };
        }

        private static string MapXmlStatus(string status)
        {
            return status switch
            {
                "Approved" => "A",
                "Rejected" => "R",
                "Done" => "D",
                _ => throw new ArgumentException("Unknown status")
            };
        }

        private static bool ValidateTransaction(Transaction transaction, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(transaction.TransactionId) || transaction.TransactionId.Length > 50)
                errorMessage = "Invalid Transaction Id";

            if (transaction.Amount <= 0)
                errorMessage = "Invalid Amount";

            if (string.IsNullOrWhiteSpace(transaction.CurrencyCode) || transaction.CurrencyCode.Length != 3)
                errorMessage = "Invalid Currency Code";

            if (transaction.TransactionDate == default(DateTime))
                errorMessage = "Invalid Transaction Date";

            if (!new[] { "A", "R", "D" }.Contains(transaction.Status))
                errorMessage = "Invalid Status";

            return string.IsNullOrEmpty(errorMessage);
        }

        public async Task SaveTransactionsAsync(List<Transaction> transactions)
        {
            await _context.Transactions.AddRangeAsync(transactions);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Transaction>> GetTransactionsAsync(string currency, DateTime? startDate, DateTime? endDate, string status)
        {
            var query = _context.Transactions.AsQueryable();

            if (!string.IsNullOrEmpty(currency))
                query = query.Where(t => t.CurrencyCode == currency);

            if (startDate.HasValue)
                query = query.Where(t => t.TransactionDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.TransactionDate <= endDate.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);

            return await query.ToListAsync();
        }
    }

}

