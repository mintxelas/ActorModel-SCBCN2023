using ActorModel.Domain.Banking;

namespace ActorModel.Persistence.Banking
{
    public class AccountsRepository: IAccountsRepository
    {
        private readonly BankingContext context;

        public AccountsRepository(BankingContext context)
        {
            this.context = context;
        }

        public Account ByReference(AccountReference reference)
        {
            var record = context.Accounts.Single(p => p.Reference == reference.Value);
            return new Account(reference, new Money(record.Balance, new CurrencyCode(record.CurrencyCode)));
        }

        public void UpdateBalance(params Account[] accounts)
        {
            foreach (var account in accounts)
            {
                var entity = context.Accounts.Single(a => a.Reference == account.Reference.Value);
                entity.Balance = account.Balance.Amount;
            }

            context.SaveChanges();

        }
    }
}