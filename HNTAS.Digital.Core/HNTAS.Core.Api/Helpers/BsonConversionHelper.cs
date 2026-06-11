using MongoDB.Bson;

namespace HNTAS.Core.Api.Helpers
{
    public static class BsonConversionHelper
    {
        /// <summary>
        /// Safely attempts to extract a double value from a BsonValue, handling numeric variations and strings.
        /// </summary>
        public static bool TryGetDouble(BsonValue? bsonValue, out double result)
        {
            if (bsonValue == null || bsonValue.IsBsonNull)
            {
                result = 0;
                return false;
            }

            if (bsonValue.IsNumeric)
            {
                // Safely handles BsonInt32, BsonInt64, BsonDecimal128, and BsonDouble
                result = bsonValue.ToDouble();
                return true;
            }

            if (bsonValue.IsString)
            {
                return double.TryParse(bsonValue.AsString, out result);
            }

            result = 0;
            return false;
        }
    }
}
