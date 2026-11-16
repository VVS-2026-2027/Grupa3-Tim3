using System.ComponentModel.DataAnnotations;

namespace Aplikacija.Tests.Helpers;

public static class Validacija
{
    // Poziv Validator-a je potreban: samo postavljanje svojstva ne pokrece atribute.
    public static List<ValidationResult> ProvjeriSvojstvo(object model, string naziv)
    {
        var rezultati = new List<ValidationResult>();
        var kontekst = new ValidationContext(model) { MemberName = naziv };
        object? vrijednost = model.GetType().GetProperty(naziv)!.GetValue(model);
        Validator.TryValidateProperty(vrijednost, kontekst, rezultati);
        return rezultati;
    }
}
