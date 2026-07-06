using HNTAS.Core.Api.Helpers;
using MongoDB.Bson;

namespace HNTAS.Digital.Core.Tests.Helpers
{
    public class BsonConversionHelperTests
    {

        [Fact]
        public void TryGetDouble_ReturnsFalse_WhenValueIsNull()
        {
            // Act
            var success = BsonConversionHelper.TryGetDouble(null, out var result);

            // Assert
            Assert.False(success);
            Assert.Equal(0, result);
        }

        [Fact]
        public void TryGetDouble_ReturnsFalse_WhenValueIsBsonNull()
        {
            // Act
            var success = BsonConversionHelper.TryGetDouble(BsonNull.Value, out var result);

            // Assert
            Assert.False(success);
            Assert.Equal(0, result);
        }


        [Theory]
        [InlineData(10)]
        [InlineData(0)]
        [InlineData(-5)]
        public void TryGetDouble_ReturnsTrue_ForBsonInt32(int value)
        {
            var bsonValue = new BsonInt32(value);

            var success = BsonConversionHelper.TryGetDouble(bsonValue, out var result);

            Assert.True(success);
            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData(123456789L)]
        [InlineData(-999999999L)]
        public void TryGetDouble_ReturnsTrue_ForBsonInt64(long value)
        {
            var bsonValue = new BsonInt64(value);

            var success = BsonConversionHelper.TryGetDouble(bsonValue, out var result);

            Assert.True(success);
            Assert.Equal(value, result);
        }

        [Fact]
        public void TryGetDouble_ReturnsTrue_ForBsonDouble()
        {
            var bsonValue = new BsonDouble(12.34);

            var success = BsonConversionHelper.TryGetDouble(bsonValue, out var result);

            Assert.True(success);
            Assert.Equal(12.34, result);
        }

        [Fact]
        public void TryGetDouble_ReturnsTrue_ForBsonDecimal128()
        {
            var bsonValue = new BsonDecimal128(Decimal128.Parse("99.99"));

            var success = BsonConversionHelper.TryGetDouble(bsonValue, out var result);

            Assert.True(success);
            Assert.Equal(99.99, result);
        }


        [Fact]
        public void TryGetDouble_ReturnsTrue_WhenStringIsNumeric()
        {
            var bsonValue = new BsonString("123.45");

            var success = BsonConversionHelper.TryGetDouble(bsonValue, out var result);

            Assert.True(success);
            Assert.Equal(123.45, result);
        }

        [Fact]
        public void TryGetDouble_ReturnsFalse_WhenStringIsNotNumeric()
        {
            var bsonValue = new BsonString("not-a-number");

            var success = BsonConversionHelper.TryGetDouble(bsonValue, out var result);

            Assert.False(success);
            Assert.Equal(0, result);
        }

        [Fact]
        public void TryGetDouble_ReturnsFalse_ForUnsupportedBsonType()
        {
            var bsonValue = new BsonBoolean(true);

            var success = BsonConversionHelper.TryGetDouble(bsonValue, out var result);

            Assert.False(success);
            Assert.Equal(0, result);
        }
    }
}
