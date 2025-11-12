using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Services
{
    public class UserRoleProjection
    {

        [BsonElement("firstName")]
        public string FirstName { get; set; } = null!;

        [BsonElement("lastName")]
        public string LastName { get; set; } = null!;

        // This is the role ID extracted from hnRoleMappings
        [BsonElement("roleId")]
        public int RoleId { get; set; }
    }
}