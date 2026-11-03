using System.Globalization;

namespace Vixly.PrintHelper;

// =========================================================
// FILE: Texts.cs
//
// PURPOSE:
// The words of the print helper in English, French and Spanish (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Give the phrase for a key in the phone's language
//
// IMPORTANT NOTES:
// - Short phrases only; the app has one page
// =========================================================
public static class Texts
{
    private static readonly Dictionary<string, string[]> All = new(StringComparer.Ordinal)
    {
        ["join"] = new[] { "Scan the code shown on the Printers page.", "Scannez le code affiché sur la page Imprimantes.", "Escanee el código que aparece en la página Impresoras." },
        ["scan"] = new[] { "Scan the code", "Scanner le code", "Escanear el código" },
        ["or_type"] = new[] { "Or type the company's address and the code:", "Ou saisissez l'adresse de l'entreprise et le code :", "O escriba la dirección de la empresa y el código:" },
        ["address"] = new[] { "Company address", "Adresse de l'entreprise", "Dirección de la empresa" },
        ["code"] = new[] { "Code", "Code", "Código" },
        ["join_button"] = new[] { "Join", "Rejoindre", "Unirse" },
        ["join_failed"] = new[] { "This code did not work. Make a new one on the Printers page.", "Ce code n'a pas fonctionné. Créez-en un nouveau sur la page Imprimantes.", "Este código no funcionó. Cree uno nuevo en la página Impresoras." },
        ["joined"] = new[] { "Printing for {0}.", "Impression pour {0}.", "Imprimiendo para {0}." },
        ["find"] = new[] { "Find printers nearby", "Chercher les imprimantes proches", "Buscar impresoras cercanas" },
        ["choose"] = new[] { "Choose the printer next to you:", "Choisissez l'imprimante à côté de vous :", "Elija la impresora junto a usted:" },
        ["none_found"] = new[] { "No printer found. Turn it on, check its Bluetooth, and try again.", "Aucune imprimante trouvée. Allumez-la, vérifiez son Bluetooth et réessayez.", "No se encontró ninguna impresora. Enciéndala, compruebe su Bluetooth e inténtelo de nuevo." },
        ["printer"] = new[] { "Printer: {0}", "Imprimante : {0}", "Impresora: {0}" },
        ["shipping"] = new[] { "This printer holds shipping labels (4 x 6 in)", "Cette imprimante contient des étiquettes d'expédition (4 x 6 po)", "Esta impresora tiene etiquetas de envío (4 x 6 pulg.)" },
        ["status"] = new[] { "Printed today: {0}. Failed: {1}.", "Imprimées aujourd'hui : {0}. Échecs : {1}.", "Impresas hoy: {0}. Fallidas: {1}." },
        ["stopped"] = new[] { "This phone was stopped on the Printers page. Join again with a new code.", "Ce téléphone a été arrêté sur la page Imprimantes. Rejoignez avec un nouveau code.", "Este teléfono se detuvo en la página Impresoras. Vuelva a unirse con un código nuevo." },
        ["keep_open"] = new[] { "Keep this app open while you print.", "Gardez cette application ouverte pendant l'impression.", "Mantenga esta aplicación abierta mientras imprime." },
        ["leave"] = new[] { "Leave this warehouse", "Quitter cet entrepôt", "Salir de este almacén" }
    };

    public static string Get(string key, params object[] values)
    {
        string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        int index = language == "fr" ? 1 : language == "es" ? 2 : 0;
        string text = All.TryGetValue(key, out string[]? phrases) ? phrases[index] : key;
        return values.Length == 0 ? text : string.Format(CultureInfo.CurrentCulture, text, values);
    }
}
