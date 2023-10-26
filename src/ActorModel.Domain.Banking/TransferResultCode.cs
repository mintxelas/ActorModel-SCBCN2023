namespace ActorModel.Domain.Banking;

public class TransferResultCode
{
    public string Code { get; }
    public bool IsSuccess { get; }
    public string Description { get; }

    private TransferResultCode(string code, bool isSuccess, string description)
    {
        Code = code;
        IsSuccess = isSuccess;
        Description = description;
    }

    public static readonly TransferResultCode SUCCESS = new("OK", true, "Transfer completed");

    public static readonly TransferResultCode INVALID_SOURCE_CURRENCY = new("INVALID", false, "Source currency must the account's currency");

    public static readonly TransferResultCode INSUFFICIENT_FUNDS = new("NOFUNDS", false, "Insufficient funds for transfer");

    public static readonly TransferResultCode NO_EXCHANGE_RATE = new("NORATE", false, "Exchange rate not found");
}