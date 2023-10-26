namespace ActorModel.Domain.Banking;

public interface IAccountsRepository
{
    Account ByReference(AccountReference reference);

    void UpdateBalance(params Account[] accounts);
}