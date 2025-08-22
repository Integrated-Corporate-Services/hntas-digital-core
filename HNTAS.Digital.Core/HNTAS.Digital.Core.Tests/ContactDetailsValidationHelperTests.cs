using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HNTAS.Digital.Core.Tests
{
    public class ContactDetailsValidationHelperTests
    {

        [Fact]
        public void LandlinePreferred_WithValidLandline_ReturnsLandlineAndExtension()
        {
            // Arrange
            var modelState = new ModelStateDictionary();
            var landline = "02012345678";
            var extension = "123";

            // Act
            var result = ContactDetailsValidationHelper.GetValidatedContactDetails(
                PreferredContactType.Landline,
                landline,
                extension,
                mobileNumber: "07123456789",
                modelState);

            // Assert
            Assert.Equal(landline, result.LandlineNumber);
            Assert.Equal(extension, result.ContactNumberExtension);
            Assert.Null(result.MobileNumber);
            Assert.False(modelState.ContainsKey(nameof(result.MobileNumber)));
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public void LandlinePreferred_WithMissingLandline_AddsModelError()
        {
            var modelState = new ModelStateDictionary();

            var result = ContactDetailsValidationHelper.GetValidatedContactDetails(
                PreferredContactType.Landline,
                landlineNumber: null,
                contactNumberExtension: "123",
                mobileNumber: "07123456789",
                modelState);

            Assert.Null(result.LandlineNumber);
            Assert.Equal("123", result.ContactNumberExtension);
            Assert.Null(result.MobileNumber);
            Assert.True(modelState.ContainsKey(nameof(result.LandlineNumber)));
            Assert.Equal("Enter your landline number.", modelState[nameof(result.LandlineNumber)].Errors[0].ErrorMessage);
        }

        [Fact]
        public void MobilePreferred_WithValidMobile_ReturnsMobileOnly()
        {
            var modelState = new ModelStateDictionary();
            var mobile = "07123456789";

            var result = ContactDetailsValidationHelper.GetValidatedContactDetails(
                PreferredContactType.Mobile,
                landlineNumber: "02012345678",
                contactNumberExtension: "123",
                mobileNumber: mobile,
                modelState);

            Assert.Null(result.LandlineNumber);
            Assert.Null(result.ContactNumberExtension);
            Assert.Equal(mobile, result.MobileNumber);
            Assert.False(modelState.ContainsKey(nameof(result.LandlineNumber)));
            Assert.False(modelState.ContainsKey(nameof(result.ContactNumberExtension)));
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public void MobilePreferred_WithMissingMobile_AddsModelError()
        {
            var modelState = new ModelStateDictionary();

            var result = ContactDetailsValidationHelper.GetValidatedContactDetails(
                PreferredContactType.Mobile,
                landlineNumber: "02012345678",
                contactNumberExtension: "123",
                mobileNumber: null,
                modelState);

            Assert.Null(result.LandlineNumber);
            Assert.Null(result.ContactNumberExtension);
            Assert.Null(result.MobileNumber);
            Assert.True(modelState.ContainsKey(nameof(result.MobileNumber)));
            Assert.Equal("Enter your mobile number.", modelState[nameof(result.MobileNumber)].Errors[0].ErrorMessage);
        }
    }
}
