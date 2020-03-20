using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> users { get; set; }
        public DbSet<SmsSession> smsSessions { get; set; }
        public DbSet<Card> cards { get; set; }
        public DbSet<HistoryOperations> historyOperations { get; set; }
        public DbSet<AuthSession> authSessions { get; set; }
        private DbSet<HotpCounter> hotpCounter { get; set; }

        protected DbContextOptions<AppDbContext> _dbContextOptions;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<HotpCounter>(e =>
            {
                e.HasNoKey();
            });
        }

        public long GetHotpCounter()
        {
            var sequence = this.hotpCounter.FromSqlRaw("SELECT nextval('public.hotpcounter') as Value").ToList().FirstOrDefault();
            return sequence.Value;
        }
    }
}
