using RippleTreasury.Domain;

namespace RippleTreasury.Domain.Tests;

public class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Given_ANonpositiveAmount_When_CreatingMoney_Then_RejectTheAmount(long amount)
    {
        // Given: a zero or negative amount in a supported currency.
        const string currency = "GBP";

        // When
        var createMoney = () => new Money(amount, currency);

        // Then
        Assert.Throws<DomainException>(createMoney);
    }

    [Fact]
    public void Given_TooManyDecimalPlaces_When_ConvertingMoney_Then_RejectWithoutRounding()
    {
        // Given
        const decimal amount = 1.001m;

        // When
        var convert = () => Money.FromDecimal(amount, "GBP");

        // Then
        Assert.Throws<DomainException>(convert);
    }

    [Fact]
    public void Given_AnUnsupportedCurrency_When_CreatingMoney_Then_RejectTheCurrency()
    {
        // Given
        const string currency = "ZZZ";

        // When
        var createMoney = () => new Money(1, currency);

        // Then
        Assert.Throws<DomainException>(createMoney);
    }

    [Fact]
    public void Given_AnAmountBeyondTheSupportedRange_When_ConvertingMoney_Then_RejectOverflow()
    {
        // Given
        const decimal amount = decimal.MaxValue;

        // When
        var convert = () => Money.FromDecimal(amount, "GBP");

        // Then
        Assert.Throws<OverflowException>(convert);
    }

    [Fact]
    public void Given_AValidEuroAmount_When_ConvertingMoney_Then_PreserveExactMinorUnits()
    {
        // Given
        const decimal amount = 1.23m;

        // When
        var money = Money.FromDecimal(amount, "EUR");

        // Then
        Assert.Equal(123, money.MinorUnits);
    }
}
