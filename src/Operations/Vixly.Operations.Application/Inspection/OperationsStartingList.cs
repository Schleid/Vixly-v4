using Vixly.Operations.Domain.Inspection;

namespace Vixly.Operations.Application.Inspection;

// =========================================================
// FILE: OperationsStartingList.cs
//
// PURPOSE:
// The starting restricted-items list a company may copy and then own (Slice 3C, D-R5).
//
// RESPONSIBILITIES:
// - Give the starting entries with their names and what is needed in English, French or Spanish
// - Give the words that point to each, in all three languages
//
// IMPORTANT NOTES:
// - Each company copies it once and changes its copy; this list is never read again for that company
// - A starting point, not legal advice: the company checks its carriers' and destinations' rules
// =========================================================
public static class OperationsStartingList
{
    private static readonly Entry[] Entries =
    {
        new("Lithium batteries", "Batteries au lithium", "Baterías de litio", "lithium, battery, batteries, power bank, powerbank, li-ion, lipo, batterie, pile, piles, litio, batería, baterías, pila, pilas", OperationsRestrictedItemKind.Restricted,
            "Inside the device or packed as the carrier requires; no loose spare batteries by air.", "Dans l'appareil ou emballées selon le transporteur ; pas de batteries de rechange en vrac par avion.", "Dentro del aparato o embaladas según el transportista; sin baterías sueltas de repuesto por avión."),
        new("Aerosols", "Aérosols", "Aerosoles", "aerosol, spray, hairspray, spray paint, deodorant spray, bombe, aérosol, aerosoles, espray", OperationsRestrictedItemKind.Restricted,
            "Not by air; ground or ocean only, sealed and padded.", "Pas par avion ; route ou mer seulement, fermés et protégés.", "No por avión; solo terrestre o marítimo, cerrados y protegidos."),
        new("Flammable liquids", "Liquides inflammables", "Líquidos inflamables", "gasoline, petrol, lighter fluid, lighter, thinner, solvent, nail polish, flammable, essence, briquet, solvant, inflammable, gasolina, encendedor, solvente, inflamable", OperationsRestrictedItemKind.Prohibited,
            "Not accepted.", "Non accepté.", "No se acepta."),
        new("Medicine", "Médicaments", "Medicamentos", "medicine, medication, pills, tablets, capsules, prescription, vitamins, supplements, médicament, medicaments, ordonnance, vitamine, medicina, medicamento, pastillas, receta, vitaminas, suplementos", OperationsRestrictedItemKind.Restricted,
            "The prescription or the destination's import permit.", "L'ordonnance ou l'autorisation d'importation du pays de destination.", "La receta o el permiso de importación del país de destino."),
        new("Firearms and parts", "Armes à feu et pièces", "Armas de fuego y piezas", "gun, guns, firearm, pistol, rifle, shotgun, ammunition, ammo, bullets, gun parts, arme, pistolet, fusil, munitions, cartouches, arma, pistola, escopeta, municiones, balas", OperationsRestrictedItemKind.Prohibited,
            "Not accepted.", "Non accepté.", "No se acepta."),
        new("Currency", "Argent liquide", "Dinero en efectivo", "cash, currency, banknotes, money, espèces, billets de banque, argent liquide, efectivo, billetes, dinero", OperationsRestrictedItemKind.Prohibited,
            "Not accepted.", "Non accepté.", "No se acepta."),
        new("Perishable food", "Denrées périssables", "Alimentos perecederos", "food, meat, cheese, fish, seafood, fruit, vegetables, perishable, nourriture, viande, fromage, poisson, légumes, comida, carne, queso, pescado, mariscos, frutas, verduras", OperationsRestrictedItemKind.Restricted,
            "Refrigerated shipment, sealed packaging; the destination's food rules.", "Envoi réfrigéré, emballage scellé ; les règles alimentaires du pays de destination.", "Envío refrigerado, empaque sellado; las normas alimentarias del país de destino."),
        new("Items needing a permit", "Articles soumis à autorisation", "Artículos que requieren permiso", "plants, seeds, drone, transmitter, satellite phone, walkie talkie, plantes, graines, émetteur, téléphone satellite, plantas, semillas, transmisor, teléfono satelital", OperationsRestrictedItemKind.Restricted,
            "The destination's import permit.", "L'autorisation d'importation du pays de destination.", "El permiso de importación del país de destino.")
    };

    public static IReadOnlyList<OperationsStartingItem> In(string? language)
    {
        int index = language switch
        {
            "fr" => 1,
            "es" => 2,
            _ => 0
        };

        return Entries.Select(entry => new OperationsStartingItem(entry.Names[index], entry.Words, entry.Kind, entry.Requirements[index])).ToList();
    }

    private sealed record Entry(string[] Names, string Words, OperationsRestrictedItemKind Kind, string[] Requirements)
    {
        public Entry(string english, string french, string spanish, string words, OperationsRestrictedItemKind kind, string englishNeed, string frenchNeed, string spanishNeed)
            : this(new[] { english, french, spanish }, words, kind, new[] { englishNeed, frenchNeed, spanishNeed })
        {
        }
    }
}
