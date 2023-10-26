namespace ActorModel.Domain.Banking;

public record TransferRequest(AccountReference SourceAccount, AccountReference TargetAccount, Money Amount);