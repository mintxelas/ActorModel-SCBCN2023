using ActorModel.Domain.Banking;
using ActorModel.Persistence.Banking;
using ActorModel.Persistence.Banking.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ActorModel.Persistence.Tests
{
    public class AccountsRepositoryShould
    {
        private const int StartingBalanceAmount = 100;
        private const string Iso3LetterCode = "EUR";
        private readonly IAccountsRepository accountsRepository = new AccountsRepository(GetDbContext());

        [Fact]
        public void GetAccountById()
        {
            var givenAccount = GivenAnAccountInDatabase("a-reference");

            var actualAccount = accountsRepository.ByReference(givenAccount.Reference);

            actualAccount.Should().BeEquivalentTo(givenAccount);
        }

        [Fact]
        public void AdjustBalancesWhenTransferringFunds()
        {
            var sourceAccount = GivenAnAccountInDatabase("source-reference");
            var targetAccount = GivenAnAccountInDatabase("target-reference");
            sourceAccount.DecreaseFunds(sourceAccount.Balance);
            targetAccount.IncreaseFunds(targetAccount.Balance);

            accountsRepository.UpdateBalance(sourceAccount, targetAccount);

            const decimal sourceBalanceShouldBe = 0;
            const decimal targetBalanceShouldBe = 2 * StartingBalanceAmount;
            ThenBalancesAreUpdatedInDatabase(sourceAccount.Reference, targetAccount.Reference, sourceBalanceShouldBe, targetBalanceShouldBe);
        }

        private static Account GivenAnAccountInDatabase(string accountReference)
        {
            using var context = GetDbContext();
            var entity = new PersistentAccount
            {
                Balance = StartingBalanceAmount,
                CurrencyCode = Iso3LetterCode,
                Reference = accountReference 
            };
            context.Accounts.Add(entity);
            context.SaveChanges();
            return new Account(new AccountReference(accountReference),
                new Money(StartingBalanceAmount, new CurrencyCode(Iso3LetterCode)));
        }

        private static void ThenBalancesAreUpdatedInDatabase(AccountReference sourceAccountReference, AccountReference targetAccountReference, decimal sourceBalanceShouldBe, decimal targetBalanceShouldBe)
        {
            using var context = GetDbContext();

            var sourceAccountBalance = context.Accounts
                .Where(a => a.Reference == sourceAccountReference.Value)
                .Select(a => a.Balance).Single();
            sourceAccountBalance.Should().Be(sourceBalanceShouldBe);

            var targetAccountBalance = context.Accounts
                .Where(a => a.Reference == targetAccountReference.Value)
                .Select(a => a.Balance).Single();
            targetAccountBalance.Should().Be(targetBalanceShouldBe);
        }

        private static BankingContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<BankingContext>()
                .UseInMemoryDatabase("banking-accounts-tests")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            return new BankingContext(optionsBuilder.Options);

        }
    }
}