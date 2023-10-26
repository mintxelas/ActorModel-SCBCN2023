namespace ActorModel.Domain.Banking;

public record TransferResponse(TransferResultCode Code, Money? TransferredAmount = null);