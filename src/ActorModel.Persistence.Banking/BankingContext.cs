using ActorModel.Persistence.Banking.Entities;
using Microsoft.EntityFrameworkCore;

namespace ActorModel.Persistence.Banking;

public class BankingContext: DbContext
{
    public DbSet<PersistentAccount> Accounts { get; set; }

    public DbSet<PersistentExchangeRate> ExchangeRates { get; set; }
    
    public BankingContext(DbContextOptions<BankingContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PersistentAccount>(build =>
        {
            build.HasKey(entry => entry.Reference);
        });

        modelBuilder.Entity<PersistentExchangeRate>(build =>
        {
            build.HasKey(entry => new { entry.SourceCurrency, entry.TargetCurrency });
        });
    }

    private const string LockConnectionString = "Host=localhost;Database=postgres;Username=postgres;Password=postgres";
    private const string ConnectionString = "Host=localhost;Database=banking;Username=postgres;Password=postgres";

    public static DbLock GetDbLock(string lockName) => new (lockName, LockConnectionString);
    
    public static BankingContext CreateInstance()
    {
        var optionsBuilder = new DbContextOptionsBuilder<BankingContext>()
            .UseNpgsql(ConnectionString);
        return new BankingContext(optionsBuilder.Options);
    }

    public static void SeedDatabase(bool forceDelete = false)
    {
        var dbLock = GetDbLock("database-start");
        dbLock.Lock( () => {
            using var context = CreateInstance();
            
            if (forceDelete) context.Database.EnsureDeleted();

            if (!context.Database.EnsureCreated()) return;

            for (var i = 1; i <= 500; i++)
            {
                context.Accounts.Add(new PersistentAccount
                {
                    CurrencyCode = "EUR",
                    Balance = 1000,
                    Reference = $"ACCOUNT_{i}"
                });
            }

            for (var i = 501; i <= 1000; i++)
            {
                context.Accounts.Add(new PersistentAccount
                {
                    CurrencyCode = "GBP",
                    Balance = 200,
                    Reference = $"ACCOUNT_{i}"
                });
            }

            context.ExchangeRates.Add(new PersistentExchangeRate
            {
                Rate = 0.5m,
                SourceCurrency = "EUR",
                TargetCurrency = "GBP"
            });

            context.ExchangeRates.Add(new PersistentExchangeRate
            {
                Rate = 2m,
                SourceCurrency = "GBP",
                TargetCurrency = "EUR"
            });

            context.SaveChanges();
        });
    }
}