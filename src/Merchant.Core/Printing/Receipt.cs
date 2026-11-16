using System.Globalization;
using System.Net.Sockets;
using System.Text;
using Merchant.Core.Domain;

namespace Merchant.Core.Printing;

/// <summary>Renders a sale as ESC/POS bytes for a 58 mm (32 column) receipt printer.</summary>
public static class Receipt
{
    private const int Columns = 32;
    private static readonly byte[] Initialise = [0x1B, 0x40];
    private static readonly byte[] AlignCentre = [0x1B, 0x61, 0x01];
    private static readonly byte[] AlignLeft = [0x1B, 0x61, 0x00];
    private static readonly byte[] BoldOn = [0x1B, 0x45, 0x01];
    private static readonly byte[] BoldOff = [0x1B, 0x45, 0x00];
    private static readonly byte[] FeedAndCut = [0x1D, 0x56, 0x42, 0x03];
    private static readonly byte[] KickDrawer = [0x1B, 0x70, 0x00, 0x19, 0xFA];

    public static byte[] Build(string shopName, SaleCompleted sale, DateTimeOffset when)
    {
        using var buffer = new MemoryStream();
        void Raw(byte[] bytes) => buffer.Write(bytes);
        // Printers default to a single-byte code page; anything outside ASCII prints as '?'.
        void Line(string text) => buffer.Write(Encoding.ASCII.GetBytes(text + "\n"));

        Raw(Initialise);
        Raw(AlignCentre);
        Raw(BoldOn);
        Line(shopName);
        Raw(BoldOff);
        Line(when.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        Raw(AlignLeft);
        Line(new string('-', Columns));
        foreach (var line in sale.Lines)
            Line(Row($"{line.Quantity} x {line.Name}", Money(line.TotalCents)));
        Line(new string('-', Columns));
        Raw(BoldOn);
        Line(Row("TOTAL", Money(sale.TotalCents)));
        Raw(BoldOff);
        Line(Row("Paid by", sale.Tender));
        Raw(FeedAndCut);
        // The cash drawer plugs into the printer and opens on this pulse.
        if (sale.Tender == Tender.Cash)
            Raw(KickDrawer);
        return buffer.ToArray();
    }

    private static string Money(long cents) => (cents / 100m).ToString("0.00", CultureInfo.InvariantCulture);

    private static string Row(string left, string right)
    {
        var room = Columns - right.Length - 1;
        if (left.Length > room)
            left = left[..room];
        return left.PadRight(Columns - right.Length) + right;
    }
}

/// <summary>Sends raw bytes to a network receipt printer (the standard raw port is 9100).</summary>
public static class NetworkPrinter
{
    public static async Task SendAsync(string hostAndPort, byte[] data, CancellationToken ct = default)
    {
        var parts = hostAndPort.Trim().Split(':', 2);
        var port = 9100;
        if (parts.Length == 2 && !int.TryParse(parts[1], CultureInfo.InvariantCulture, out port))
            throw new FormatException($"'{hostAndPort}' is not a printer address like 192.168.1.50 or 192.168.1.50:9100.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        using var client = new TcpClient();
        await client.ConnectAsync(parts[0], port, timeout.Token);
        await client.GetStream().WriteAsync(data, timeout.Token);
    }
}
