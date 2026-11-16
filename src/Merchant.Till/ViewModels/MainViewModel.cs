using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Merchant.Core.Domain;
using Merchant.Core.Printing;

namespace Merchant.Till.ViewModels;

public partial class ProductButton(Product product, Action<Product> tapped) : ViewModelBase
{
    public Product Product => product;
    public string Name => product.Name;
    public string Price => MainViewModel.Money(product.PriceCents);

    [RelayCommand]
    private void Tap() => tapped(product);
}

public partial class CartLine(Product product) : ViewModelBase
{
    public Product Product => product;
    public string Name => product.Name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    public partial int Quantity { get; set; } = 1;

    public string Total => MainViewModel.Money(Quantity * product.PriceCents);
}

public partial class MainViewModel : ViewModelBase
{
    private readonly TillRuntime _runtime;

    public MainViewModel(TillRuntime runtime)
    {
        _runtime = runtime;
        PrinterHost = runtime.PrinterHost;
        PairingCode = runtime.PairingCode;
        Refresh();

        // Both can fire on a background thread when another till syncs with this one.
        runtime.Store.Changed += OnStoreChanged;
        runtime.SyncStatusChanged += status => Dispatcher.UIThread.Post(() => SyncStatus = status);
    }

    public string TillName => _runtime.Name;
    public ObservableCollection<ProductButton> Products { get; } = [];
    public ObservableCollection<CartLine> Cart { get; } = [];

    /// <summary>Set by the window: asks where to save, or which file to open. Null when cancelled.</summary>
    public Func<Task<string?>>? PickBackupToSave { get; set; }
    public Func<Task<string?>>? PickBackupToOpen { get; set; }

    [ObservableProperty] public partial string NewProductName { get; set; } = "";
    [ObservableProperty] public partial string NewProductPrice { get; set; } = "";
    [ObservableProperty] public partial string CartTotal { get; set; } = Money(0);
    [ObservableProperty] public partial string TodaySummary { get; set; } = "";
    [ObservableProperty] public partial string Status { get; set; } = "";
    [ObservableProperty] public partial string SyncStatus { get; set; } = "Looking for other tills...";
    [ObservableProperty] public partial string PairingCode { get; set; }
    [ObservableProperty] public partial string JoinCode { get; set; } = "";
    [ObservableProperty] public partial string PrinterHost { get; set; }
    [ObservableProperty] public partial string BackupPassphrase { get; set; } = "";

    public static string Money(long cents) => (cents / 100m).ToString("C", CultureInfo.CurrentCulture);

    /// <summary>Reads "12", "12.5" or "12,50" as cents, whatever the machine's regional settings.</summary>
    public static bool TryParsePrice(string text, out long cents)
    {
        cents = 0;
        if (!decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var amount))
            return false;
        if (amount < 0 || amount > 1_000_000 || decimal.Round(amount, 2) != amount)
            return false;
        cents = (long)(amount * 100);
        return true;
    }

    [RelayCommand]
    private void AddProduct()
    {
        var name = NewProductName.Trim();
        if (name.Length == 0 || !TryParsePrice(NewProductPrice, out var cents))
        {
            Status = "Enter a product name and a price such as 45 or 45.50.";
            return;
        }

        _runtime.Store.Append(EventTypes.ProductDefined,
            ShopJson.Write(new ProductDefined(Guid.CreateVersion7(), name, cents)));
        NewProductName = "";
        NewProductPrice = "";
        Status = $"Added {name}.";
    }

    private void AddToCart(Product product)
    {
        if (Cart.FirstOrDefault(l => l.Product.Id == product.Id) is { } line)
            line.Quantity++;
        else
            Cart.Add(new CartLine(product));
        UpdateCartTotal();
    }

    [RelayCommand]
    private void ClearCart()
    {
        Cart.Clear();
        UpdateCartTotal();
    }

    [RelayCommand]
    private Task PayCash() => CompleteSale(Tender.Cash);

    [RelayCommand]
    private Task PayCard() => CompleteSale(Tender.Card);

    private async Task CompleteSale(string tender)
    {
        if (Cart.Count == 0)
        {
            Status = "The cart is empty.";
            return;
        }

        var lines = Cart
            .Select(l => new SaleLine(l.Product.Id, l.Product.Name, l.Quantity, l.Product.PriceCents))
            .ToList();
        var sale = new SaleCompleted(Guid.CreateVersion7(), lines, lines.Sum(l => l.TotalCents), tender);
        var record = _runtime.Store.Append(EventTypes.SaleCompleted, ShopJson.Write(sale));

        Cart.Clear();
        UpdateCartTotal();
        Status = $"Sale of {Money(sale.TotalCents)} saved ({tender}).";

        // The sale is already safe on disk; a printer that is off or out of paper must not undo it.
        var printer = _runtime.PrinterHost;
        if (printer.Length == 0)
            return;
        try
        {
            await NetworkPrinter.SendAsync(printer, Receipt.Build("Merchant", sale, record.OccurredAt));
        }
        catch (Exception e)
        {
            Status = $"Sale of {Money(sale.TotalCents)} saved, but the receipt did not print: {e.Message}";
        }
    }

    [RelayCommand]
    private async Task SyncNow()
    {
        try
        {
            await _runtime.SyncNowAsync();
        }
        catch (Exception e)
        {
            SyncStatus = "Sync failed: " + e.Message;
        }
    }

    [RelayCommand]
    private void JoinShop()
    {
        if (!_runtime.JoinShop(JoinCode))
        {
            Status = "That pairing code is not complete. It has 32 letters and digits.";
            return;
        }
        PairingCode = _runtime.PairingCode;
        JoinCode = "";
        Status = "Joined the shop. This till now syncs with its other tills.";
    }

    [RelayCommand]
    private void SavePrinter()
    {
        _runtime.PrinterHost = PrinterHost;
        Status = _runtime.PrinterHost.Length == 0 ? "Receipt printing is off." : $"Receipts print to {_runtime.PrinterHost}.";
    }

    [RelayCommand]
    private async Task ExportBackup()
    {
        if (string.IsNullOrWhiteSpace(BackupPassphrase))
        {
            Status = "Type a backup passphrase first. You need it to restore the backup.";
            return;
        }
        if (PickBackupToSave is null || await PickBackupToSave() is not { } path)
            return;
        try
        {
            _runtime.Store.ExportBackup(path, BackupPassphrase);
            Status = $"Backup saved to {path}.";
        }
        catch (Exception e)
        {
            Status = "Backup failed: " + e.Message;
        }
    }

    [RelayCommand]
    private async Task RestoreBackup()
    {
        if (PickBackupToOpen is null || await PickBackupToOpen() is not { } path)
            return;
        try
        {
            var imported = _runtime.Store.RestoreBackup(path, BackupPassphrase);
            PairingCode = _runtime.PairingCode;
            Status = $"Restored {imported} new record(s) from the backup.";
        }
        catch (Exception e)
        {
            Status = "Restore failed: " + e.Message;
        }
    }

    private void UpdateCartTotal() =>
        CartTotal = Money(Cart.Sum(l => l.Quantity * l.Product.PriceCents));

    private void OnStoreChanged()
    {
        if (Dispatcher.UIThread.CheckAccess())
            Refresh();
        else
            Dispatcher.UIThread.Post(Refresh);
    }

    /// <summary>Rebuilds what the screen shows from the event log.</summary>
    public void Refresh()
    {
        var state = ShopState.From(_runtime.Store.ReadAll());
        Products.Clear();
        foreach (var product in state.Products)
            Products.Add(new ProductButton(product, AddToCart));

        var today = DateOnly.FromDateTime(DateTime.Now);
        TodaySummary = $"Today: {Money(state.TotalCentsOn(today))} in {state.SaleCountOn(today)} sale(s)";
    }
}
