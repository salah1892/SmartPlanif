using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Metier
{

    /// <summary>
    /// Règles tarifaires
    /// </summary>
    public class FareRule
    {
        // Clé composite configurée dans le DbContext
        [Column("FARE_ID")] public int FareId { get; set; }
        [Column("ROUTE_ID")] public int RouteId { get; set; } // CORRIGÉ : smallint -> short
        [Column("ORIGIN_ID")] public int OriginId { get; set; }
        [Column("DESTINATION_ID")] public int DestinationId { get; set; }
        [Column("DU_ID")] public int DuId { get; set; }
        [Column("AU_ID")] public int AuId { get; set; }
        [Column("TYPE_TARIF")] public string TypeTarif { get; set; } = string.Empty;
        [Column("SECTION")] public string? Section { get; set; }
    }
}