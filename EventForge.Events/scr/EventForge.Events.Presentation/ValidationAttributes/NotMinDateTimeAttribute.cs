using System.ComponentModel.DataAnnotations;

namespace EventForge.Events.Presentation.ValidationAttributes;

/// <summary>
/// Атрибут проверки, что дата не заполнена как "0001-01-01T00:00:00"
/// Такое бывает, если свойство из DTO вообще убрали
/// </summary>
public class NotMinDateTimeAttribute : ValidationAttribute
{
    /// <inheritdoc/>
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTime dateTime && dateTime == DateTime.MinValue)
        {
            return new ValidationResult(ErrorMessage ?? "Значение даты не может быть минимальной датой (0001-01-01).");
        }
        return ValidationResult.Success!;
    }
}
