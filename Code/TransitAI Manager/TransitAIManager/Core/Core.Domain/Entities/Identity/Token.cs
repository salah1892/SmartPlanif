using Core.Domain.Common;

namespace Core.Domain.Entities.Identity
{
    public class Token: BaseEntity
    {
        public string? UserId { get; set; }
        public string? RefreshToken { get; set; }
        public DateTimeOffset ExpiryDate { get; set; }

        //[Timestamp] // Attribut EF Core pour le verrouillage optimiste
        //public byte[]? Version { get; set; }
    }
}