namespace ActorModel.Persistence.Banking.Entities;

public class PersistentExchangeRate
{
    public string SourceCurrency { get; set; }
    public string TargetCurrency { get; set; }
    public decimal Rate { get; set; }
}