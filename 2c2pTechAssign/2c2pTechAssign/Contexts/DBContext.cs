using System;
using _2c2pTechAssign.Models;
using Microsoft.EntityFrameworkCore;

namespace _2c2pTechAssign.Contexts
{
    public class DBContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<ValidationLog> ValidationLogs { get; set; }

        public DBContext(DbContextOptions<DBContext> options)
            : base(options)
        {
        }
    }
}
