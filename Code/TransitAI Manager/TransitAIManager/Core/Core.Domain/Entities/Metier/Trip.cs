using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// Trajets effectués
    /// </summary>
    public class Trip
    {
        // Clé composite configurée dans le DbContext
        [Column("DEDATED")] public DateTime Dedated { get; set; }
        [Column("TRIP_ID")] public int TripId { get; set; }
        [Column("DENUMLI")] public int Denomli { get; set; }
        [Column("SERVICE_ID")] public int? ServiceId { get; set; }
        [Column("DIRECTION_ID")] public int DirectionId { get; set; }

        [Column("TIME_DEPART")] public DateTime? TimeDepart { get; set; }
        [Column("HAVERET")] public int? Haveret { get; set; }
        [Column("TIME_NRET")] public string? TimeNret { get; set; }
        [Column("TRIP_NID")] public int? TripNid { get; set; }
        [Column("GRP")] public int? Grp { get; set; }
        [Column("BUS_PR")] public int BusPr { get; set; }
        [Column("BUS_RE")] public int BusRe { get; set; }
        [Column("CHAUFF_PR")] public int? ChauffPr { get; set; }
        [Column("CHAUFF_RE")] public int? ChauffRe { get; set; }
        [Column("REC_PR")] public int? RecPr { get; set; }
        [Column("REC_RE")] public int? RecRe { get; set; }
        [Column("ETAT")] public int? Etat { get; set; }

        [Column("TIME_DEPART_R")] public DateTime? TimeDepartR { get; set; }
        [Column("TIME_ARRIVE_R")] public DateTime? TimeArriveR { get; set; }
        [Column("VMAX")] public int? Vmax { get; set; }
        [Column("AVANCE_RETARD")] public int AvanceRetard { get; set; }
        [Column("CHANGEMENT")] public int Changement { get; set; }
        [Column("META_DATA")] public string? MetaData { get; set; }
        [Column("DEVALID")] public int Devalid { get; set; }
        [Column("ALERT")] public int Alert { get; set; }
    }
}