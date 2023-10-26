using ActorModel.Domain.Banking;
using FluentAssertions;
using NSubstitute;

namespace ActorModel.Domain.Tests
{
    public class TransferServiceShould
    {
        private const int SourceInitialBalance = 100;
        private const int TargetInitialBalance = 0;
        private const string SourceCurrencyCode = "EUR";
        private const string DifferentCurrencyCode = "GBP";
        private readonly TransferService transferManager;
        private readonly Account sourceAccount;
        private readonly Account targetAccount;
        private readonly IAccountsRepository accountsRepository;
        private readonly IExchangeRepository exchangeRepository;

        public TransferServiceShould()
        {
            accountsRepository = Substitute.For<IAccountsRepository>();
            exchangeRepository = Substitute.For<IExchangeRepository>();
            transferManager = new TransferService(accountsRepository, exchangeRepository);
            sourceAccount = GivenAccount("source", SourceInitialBalance);
            targetAccount = GivenAccount("target", TargetInitialBalance);
            accountsRepository.ByReference(sourceAccount.Reference).Returns(sourceAccount);
            accountsRepository.ByReference(targetAccount.Reference).Returns(targetAccount);
        }

        [Fact]
        public void RejectTransferWhenSourceAccountCurrencyAndTransferCurrencyDiffer()
        {
            var transferRequest = new TransferRequest(sourceAccount.Reference, targetAccount.Reference,
                new Money(SourceInitialBalance, new CurrencyCode(DifferentCurrencyCode)));

            var transferResult = transferManager.Execute(transferRequest);

            transferResult.Code.Should().Be(TransferResultCode.INVALID_SOURCE_CURRENCY);
            transferResult.TransferredAmount.Should().BeNull();
            accountsRepository.Received(0)
                .UpdateBalance(Arg.Any<Account[]>());

        }

        [Fact]
        public void RejectTransferWhenSourceAccountHasInsufficientFunds()
        {
            var transferRequest = CreateTransferRequest(targetAccount.Reference, 2*SourceInitialBalance);

            var transferResult = transferManager.Execute(transferRequest);

            transferResult.Code.Should().Be(TransferResultCode.INSUFFICIENT_FUNDS);
            transferResult.TransferredAmount.Should().BeNull();
            accountsRepository.Received(0)
                .UpdateBalance(Arg.Any<Account[]>());
        }

        [Fact]
        public void PerformTransferWhenSourceAccountHasEnoughFunds()
        {
            var transferRequest = CreateTransferRequest(targetAccount.Reference, SourceInitialBalance);
            exchangeRepository.GetRate(Arg.Any<CurrencyCode>(), Arg.Any<CurrencyCode>()).Returns(1);
            var transferResult = transferManager.Execute(transferRequest);

            transferResult.Code.Should().Be(TransferResultCode.SUCCESS);
            transferResult.TransferredAmount.Should().Be(transferRequest.Amount);
            accountsRepository.Received(1).UpdateBalance(sourceAccount, targetAccount);
        }

        [Fact]
        public void FailWhenExchangeRateIsNotFound()
        {
            var anotherTargetAccount = GivenAccount("another-target", TargetInitialBalance, DifferentCurrencyCode);
            accountsRepository.ByReference(anotherTargetAccount.Reference).Returns(anotherTargetAccount);
            var transferRequest = CreateTransferRequest(anotherTargetAccount.Reference, SourceInitialBalance);
            
            var transferResult = transferManager.Execute(transferRequest);

            transferResult.Code.Should().Be(TransferResultCode.NO_EXCHANGE_RATE);
            transferResult.TransferredAmount.Should().BeNull();
            accountsRepository.Received(0)
                .UpdateBalance(Arg.Any<Account[]>());
        }

        [Fact]
        public void ApplyExchangeRateWhenSourceAndTargetCurrenciesDiffer()
        {
            var anotherTargetAccount = GivenAccount("another-target", TargetInitialBalance, DifferentCurrencyCode);
            accountsRepository.ByReference(anotherTargetAccount.Reference).Returns(anotherTargetAccount);
            var transferRequest = CreateTransferRequest(anotherTargetAccount.Reference, SourceInitialBalance);
            exchangeRepository.GetRate(new CurrencyCode(SourceCurrencyCode), new CurrencyCode(DifferentCurrencyCode)).Returns(2);

            var transferResult = transferManager.Execute(transferRequest);

            transferResult.Code.Should().Be(TransferResultCode.SUCCESS);
            var expectedAmount = new Money(SourceInitialBalance * 2, new CurrencyCode(DifferentCurrencyCode));
            transferResult.TransferredAmount.Should().Be(expectedAmount);
            accountsRepository.Received(1).UpdateBalance(sourceAccount, anotherTargetAccount);
        }

        private TransferRequest CreateTransferRequest(AccountReference targetReference, decimal balance)
        {
            return new TransferRequest(sourceAccount.Reference, targetReference,
                new Money(balance, sourceAccount.Balance.Currency));
        }

        private static Account GivenAccount(string reference, decimal balance, string currencyCode = SourceCurrencyCode)
        {
            return new Account(new AccountReference(reference), new Money(balance, new CurrencyCode(currencyCode)));
        }
    }
}