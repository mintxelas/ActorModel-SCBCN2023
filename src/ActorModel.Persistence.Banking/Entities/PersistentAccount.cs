namespace ActorModel.Persistence.Banking.Entities;

public class PersistentAccount
{
    public string Reference { get; set; }
    public decimal Balance { get; set; }
    public string CurrencyCode { get; set; }
}