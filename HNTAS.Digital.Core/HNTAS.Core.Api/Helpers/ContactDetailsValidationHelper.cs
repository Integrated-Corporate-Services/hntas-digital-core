using HNTAS.Core.Api.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HNTAS.Core.Api.Helpers
{
    public class ContactDetailsValidationHelper
    {
        public static (string? LandlineNumber, string? ContactNumberExtension, string? MobileNumber) GetValidatedContactDetails(
         PreferredContactType preferredContactType,
        string? landlineNumber,
        string? contactNumberExtension,
        string? mobileNumber,
        ModelStateDictionary modelState)
        {
            string? validatedLandline = null;
            string? validatedExtension = null;
            string? validatedMobile = null;

            switch (preferredContactType)
            {
                case PreferredContactType.Landline:
                    validatedLandline = landlineNumber;
                    validatedExtension = contactNumberExtension;
                    modelState.Remove(nameof(mobileNumber));

                    if (string.IsNullOrWhiteSpace(landlineNumber))
                    {
                        modelState.AddModelError(nameof(landlineNumber), "Enter your landline number.");
                    }
                    break;

                case PreferredContactType.Mobile:
                    validatedMobile = mobileNumber;
                    modelState.Remove(nameof(landlineNumber));
                    modelState.Remove(nameof(contactNumberExtension));

                    if (string.IsNullOrWhiteSpace(mobileNumber))
                    {
                        modelState.AddModelError(nameof(mobileNumber), "Enter your mobile number.");
                    }
                    break;
            }

            return (validatedLandline, validatedExtension, validatedMobile);
        }

    }
}
