using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace HotelTravel.Web.Localization;

/// <summary>
/// [Required], [EmailAddress] kimi atributlarda ErrorMessage yazılmayıbsa,
/// standart ingiliscə mesajı açar kimi təyin edir. Belə olanda
/// DataAnnotations localizer bu mesajı da tərcümə edə bilir
/// ("The Email field is required." → "Email sahəsi mütləqdir.").
/// </summary>
public class DefaultValidationMessages : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (!string.IsNullOrEmpty(attribute.ErrorMessage) ||
                !string.IsNullOrEmpty(attribute.ErrorMessageResourceName))
                continue;

            attribute.ErrorMessage = attribute switch
            {
                RequiredAttribute => "The {0} field is required.",
                EmailAddressAttribute => "The {0} field is not a valid e-mail address.",
                PhoneAttribute => "The {0} field is not a valid phone number.",
                CompareAttribute => "'{0}' and '{1}' do not match.",
                RangeAttribute => "The field {0} must be between {1} and {2}.",
                MinLengthAttribute => "The field {0} must have at least {1} characters.",
                MaxLengthAttribute => "The field {0} must have at most {1} characters.",
                StringLengthAttribute s when s.MinimumLength > 0 => "The field {0} must be a string with a minimum length of {2} and a maximum length of {1}.",
                StringLengthAttribute => "The field {0} must be a string with a maximum length of {1}.",
                _ => attribute.ErrorMessage
            };
        }
    }
}
