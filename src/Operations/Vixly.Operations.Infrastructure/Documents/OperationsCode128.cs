namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsCode128.cs
//
// PURPOSE:
// Encodes text as a Code 128 (set B) barcode (Slice 3B, D-R3).
//
// RESPONSIBILITIES:
// - Turn printable ASCII into bar and space widths, with the check symbol and stop
//
// IMPORTANT NOTES:
// - Patterns follow ISO/IEC 15417; every symbol is 11 modules, the stop 13
// - Parcel numbers are short upper-case text, so set B is enough
// =========================================================
public static class OperationsCode128
{
    private const int StartB = 104;

    private const string Stop = "2331112";

    private static readonly string[] Patterns =
    {
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232"
    };

    public static int Checksum(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        int sum = StartB;

        for (int index = 0; index < text.Length; index++)
        {
            sum += Value(text[index]) * (index + 1);
        }

        return sum % 103;
    }

    // Module widths from the first bar: bar, space, bar, ... ending with the stop's last bar.
    public static IReadOnlyList<int> Widths(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        List<string> symbols = new() { Patterns[StartB] };
        symbols.AddRange(text.Select(character => Patterns[Value(character)]));
        symbols.Add(Patterns[Checksum(text)]);
        symbols.Add(Stop);
        return symbols.SelectMany(symbol => symbol.Select(width => width - '0')).ToList();
    }

    private static int Value(char character) =>
        character is >= ' ' and <= '~' ? character - ' ' : throw new ArgumentException("Code 128 set B takes printable ASCII only.", nameof(character));
}
