using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.Validation;

/// <summary>
/// Checkbox mütləq işarələnməlidir (məs. "sürücülük vəsiqəm var").
/// [Range(typeof(bool), "true", "true")] brauzer yoxlamasında (jQuery) səhv işləyirdi:
/// checkbox işarələnsə də "təsdiqləyin" xətası çıxırdı. Bu atribut yalnız serverdə yoxlanılır.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MustBeTrueAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is true;
}
