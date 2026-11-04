using System.Text.RegularExpressions;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Printing;

// =========================================================
// FILE: OperationsLabelPrinter.cs
//
// PURPOSE:
// A label printer at a warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - Name it, say how the print program reaches it and at which resolution it prints
//
// IMPORTANT NOTES:
// - A network address is a host or IPv4 address with a port; an installed printer is a Windows printer name
// - The print program of the same warehouse is the only one that receives its jobs
// =========================================================
public sealed class OperationsLabelPrinter : OperationsEntity
{
    public const int NameMaxLength = 60;

    public const int AddressMaxLength = 120;

    private static readonly Regex NetworkAddress = new("^[A-Za-z0-9][A-Za-z0-9.-]{0,99}:[0-9]{2,5}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private OperationsLabelPrinter()
    {
        Name = string.Empty;
        Address = string.Empty;
    }

    private OperationsLabelPrinter(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Name = string.Empty;
        Address = string.Empty;
    }

    public Guid WarehouseId { get; private set; }

    public string Name { get; private set; }

    public OperationsPrinterConnection Connection { get; private set; }

    public string Address { get; private set; }

    public int Dpi { get; private set; }

    public bool IsActive { get; private set; }

    // Slice 4D step 2: ID labels at the dock, shipping labels at inspection.
    public OperationsLabelMedia Media { get; private set; } = OperationsLabelMedia.IdLabel;

    // Slice 12E (owner H-6): the phone (Vixly Print) a portable printer belongs to; only that phone prints its labels.
    public Guid? AgentId { get; private set; }

    public static OperationsLabelPrinter ForPhone(Guid tenantId, Guid warehouseId, Guid agentId, string? name, string? address, int dpi, OperationsLabelMedia media, DateTimeOffset utcNow)
    {
        if (agentId == Guid.Empty)
        {
            throw new DomainException(OperationsPrintingErrors.AgentNotFound);
        }

        OperationsLabelPrinter printer = new(tenantId, utcNow) { WarehouseId = warehouseId, IsActive = true, AgentId = agentId };
        printer.Apply(name, OperationsPrinterConnection.Bluetooth, address, dpi, true);
        printer.Media = Checked(media);
        return printer;
    }

    public void ChangeFromPhone(string? name, int dpi, OperationsLabelMedia media, bool active, DateTimeOffset utcNow)
    {
        Apply(name, OperationsPrinterConnection.Bluetooth, Address, dpi, active);
        Media = Checked(media);
        MarkAsModified(utcNow);
    }

    public static OperationsLabelPrinter Create(Guid tenantId, Guid warehouseId, string? name, OperationsPrinterConnection connection, string? address, int dpi, DateTimeOffset utcNow,
        OperationsLabelMedia media = OperationsLabelMedia.IdLabel)
    {
        // Slice 12E: a portable printer is added by its phone only (ForPhone).
        if (connection == OperationsPrinterConnection.Bluetooth)
        {
            throw new DomainException(OperationsPrintingErrors.PhonePrinter);
        }

        OperationsLabelPrinter printer = new(tenantId, utcNow) { WarehouseId = warehouseId, IsActive = true };
        printer.Apply(name, connection, address, dpi, true);
        printer.Media = Checked(media);
        return printer;
    }

    public void Change(string? name, OperationsPrinterConnection connection, string? address, int dpi, bool active, DateTimeOffset utcNow, OperationsLabelMedia? media = null)
    {
        // Slice 12E: a phone's printer is changed from the phone; another kind never becomes one.
        if (AgentId is not null || connection == OperationsPrinterConnection.Bluetooth)
        {
            throw new DomainException(OperationsPrintingErrors.PhonePrinter);
        }

        Apply(name, connection, address, dpi, active);
        Media = media is { } chosen ? Checked(chosen) : Media;
        MarkAsModified(utcNow);
    }

    private static OperationsLabelMedia Checked(OperationsLabelMedia media) =>
        Enum.IsDefined(media) ? media : throw new DomainException(OperationsPrintingErrors.NameInvalid);

    private void Apply(string? name, OperationsPrinterConnection connection, string? address, int dpi, bool active)
    {
        string text = name?.Trim() ?? string.Empty;
        string target = address?.Trim() ?? string.Empty;

        if (text.Length is 0 or > NameMaxLength || text.Any(char.IsControl) || text.IndexOfAny(['<', '>']) >= 0)
        {
            throw new DomainException(OperationsPrintingErrors.NameInvalid);
        }

        bool reachable = connection switch
        {
            // Raw printing ports only, so a printer record cannot point the print program at another service on the warehouse network.
            OperationsPrinterConnection.Network => NetworkAddress.IsMatch(target) && int.Parse(target[(target.LastIndexOf(':') + 1)..], System.Globalization.CultureInfo.InvariantCulture) is >= 9100 and <= 9109,
            OperationsPrinterConnection.Installed => target.Length is > 0 and <= AddressMaxLength && !target.Any(char.IsControl) && target.IndexOfAny(['<', '>', '"']) < 0,
            // Slice 12E: the phone's identifier of the printer (a Bluetooth address or a system id), letters, digits, ':' and '-'.
            OperationsPrinterConnection.Bluetooth => target.Length is > 0 and <= AddressMaxLength && target.All(character => char.IsAsciiLetterOrDigit(character) || character is ':' or '-'),
            _ => false
        };

        if (!reachable)
        {
            throw new DomainException(OperationsPrintingErrors.AddressInvalid);
        }

        if (dpi is not (203 or 300))
        {
            throw new DomainException(OperationsPrintingErrors.DpiInvalid);
        }

        (Name, Connection, Address, Dpi, IsActive) = (text, connection, target, dpi, active);
    }
}
