using System.ComponentModel.DataAnnotations;
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace EventForge.Events.Presentation.ValidationAttributes;

/// <summary>
/// Атрибут проверки дат
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class DateGreaterAttribute(string comparisonProperty) : ValidationAttribute
{
    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object value, ValidationContext validationContext)
    {
        var curDate = (DateTime?) value;
        var property = validationContext.ObjectType.GetProperty(comparisonProperty) ?? throw new ArgumentException("Свойство не найдено");
        var comparisonDate = (DateTime?) property.GetValue(validationContext.ObjectInstance);

        if (curDate.HasValue && comparisonDate.HasValue && curDate < comparisonDate)
        {
            return new ValidationResult(ErrorMessage ?? "Дата окончания события не может быть раньше даты начала",
                new[] { validationContext.MemberName! }
            );
        }

        return ValidationResult.Success;
    }

    /// <inheritdoc/>
    public override bool IsValid(object value) => throw new NotImplementedException();
}