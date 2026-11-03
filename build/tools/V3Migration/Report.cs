using System.Globalization;
using System.Text;

namespace Vixly.Tools.V3Migration;

// =========================================================
// FILE: Report.cs
//
// PURPOSE:
// The reconciliation report of a migration run (Global Standard M-3).
//
// RESPONSIBILITIES:
// - Record what was copied, skipped, refused and left for the owner, by section
// - Compare V3's invoiced money with V4's
// - Print it and save it as a text file
//
// IMPORTANT NOTES:
// - Names people by their V3 display name or code only; never a password, phone number or address
// =========================================================
internal sealed class Report
{
    private readonly StringBuilder _text = new();

    private readonly List<string> _todo = new();

    private int _copied;

    private int _skipped;

    private int _problems;

    private decimal _v3Money;

    private decimal _v4Money;

    public int Problems => _problems;

    public void Section(string title, string detail) => _text.AppendLine().AppendLine("== " + title + ": " + detail);

    public void Line(string text) => _text.AppendLine(text);

    public void Copied(string what, string detail)
    {
        _copied++;
        _text.AppendLine("  copied   " + what + ": " + detail);
    }

    public void Skip(string what, string why)
    {
        _skipped++;
        _text.AppendLine("  skipped  " + what + ": " + why);
    }

    public void Problem(string what, string why)
    {
        _problems++;
        _text.AppendLine("  PROBLEM  " + what + ": " + why);
    }

    public void Todo(string text) => _todo.Add(text);

    public void Money(decimal v3, decimal v4)
    {
        _v3Money += v3;
        _v4Money += v4;
    }

    public string Render()
    {
        StringBuilder all = new();
        all.AppendLine("Vixly V3 → V4 migration, " + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture));
        all.Append(_text);
        all.AppendLine();
        all.AppendLine("== Totals: " + _copied + " copied, " + _skipped + " skipped, " + _problems + " problem(s)");
        all.AppendLine("   Invoiced money: V3 " + _v3Money.ToString("0.00", CultureInfo.InvariantCulture) + ", V4 " + _v4Money.ToString("0.00", CultureInfo.InvariantCulture)
            + (_v3Money == _v4Money ? " (equal)" : " (DIFFERENT)"));

        if (_todo.Count > 0)
        {
            all.AppendLine().AppendLine("== For the owner, in V4:");

            foreach (string item in _todo)
            {
                all.AppendLine("  - " + item);
            }
        }

        return all.ToString();
    }
}
