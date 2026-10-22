using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace TeamBoard.Api.Setup;

// Vietnamese messages for DataAnnotations, including the implicit [Required] MVC adds to non-nullable
// reference types, so DTOs keep plain attributes. The field name is already the key in "errors", so it is left out.
public class VietnameseValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (attribute.ErrorMessage is not null || attribute.ErrorMessageResourceName is not null) continue;

            attribute.ErrorMessage = attribute switch
            {
                RequiredAttribute => "Không được để trống.",
                StringLengthAttribute => "Tối đa {1} ký tự.",
                RangeAttribute { Maximum: int.MaxValue } => "Phải lớn hơn hoặc bằng {1}.",
                RangeAttribute => "Phải nằm trong khoảng {1} – {2}.",
                _ => attribute.ErrorMessage
            };
        }
    }
}
