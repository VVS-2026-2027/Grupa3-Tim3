using System.ComponentModel.DataAnnotations;

namespace Aplikacija.Tests.Helpers;

public static class ValidationHelper
{
    public static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    public static bool IsValid(object model) => Validate(model).Count == 0;
}
