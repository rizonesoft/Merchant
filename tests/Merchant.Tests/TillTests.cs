using Merchant.Core.Domain;
using Merchant.Till;
using Merchant.Till.ViewModels;

namespace Merchant.Tests;

public class TillTests
{
    [Fact]
    public async Task Ringing_up_a_sale_records_it_and_updates_the_day()
    {
        using var dir = new TempDir();
        await using var runtime = TillRuntime.Open(dir.Path);
        var till = new MainViewModel(runtime) { NewProductName = "Flat white", NewProductPrice = "35,50" };
        till.AddProductCommand.Execute(null);

        var button = Assert.Single(till.Products);
        button.TapCommand.Execute(null);
        button.TapCommand.Execute(null);
        Assert.Equal(2, Assert.Single(till.Cart).Quantity);
        Assert.Equal(MainViewModel.Money(7100), till.CartTotal);

        await till.PayCashCommand.ExecuteAsync(null);

        Assert.Empty(till.Cart);
        var saleEvent = runtime.Store.ReadAll().Single(e => e.Type == EventTypes.SaleCompleted);
        var sale = ShopJson.Read<SaleCompleted>(saleEvent.Payload);
        Assert.Equal(7100, sale.TotalCents);
        Assert.Equal(Tender.Cash, sale.Tender);
        Assert.Equal(new SaleLine(button.Product.Id, "Flat white", 2, 3550), Assert.Single(sale.Lines));
        Assert.Equal($"Today: {MainViewModel.Money(7100)} in 1 sale(s)", till.TodaySummary);
    }

    [Theory]
    [InlineData("45", 4500)]
    [InlineData("45.5", 4550)]
    [InlineData("45,50", 4550)]
    [InlineData(" 0.05 ", 5)]
    public void Prices_parse_the_same_under_any_regional_setting(string text, long cents)
    {
        Assert.True(MainViewModel.TryParsePrice(text, out var parsed));
        Assert.Equal(cents, parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-1")]
    [InlineData("1.234")]
    [InlineData("1,234.50")]
    [InlineData("abc")]
    public void Prices_that_are_not_money_are_refused(string text) =>
        Assert.False(MainViewModel.TryParsePrice(text, out _));

    [Fact]
    public async Task Joining_a_shop_takes_the_other_tills_pairing_code()
    {
        using var dir = new TempDir();
        await using var first = TillRuntime.Open(Directory.CreateDirectory(dir.File("first")).FullName);
        await using var second = TillRuntime.Open(Directory.CreateDirectory(dir.File("second")).FullName);
        Assert.NotEqual(first.PairingCode, second.PairingCode);

        Assert.False(second.JoinShop("ABCD-1234"));
        Assert.True(second.JoinShop(first.PairingCode.ToLowerInvariant().Replace('-', ' ')));
        Assert.Equal(first.ShopKey, second.ShopKey);
    }
}
