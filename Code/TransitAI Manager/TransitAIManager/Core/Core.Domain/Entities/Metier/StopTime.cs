using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// Horaires par arrêt
    /// </summary>
    public class StopTime
    {
        // Clé composite configurée dans le DbContext
        [Column("DEDATED")] public DateTime Dedated { get; set; }
        [Column("TRIP_ID")] public int TripId { get; set; }

        [Column("ARRIVAL_TIME")] public DateTime? ArrivalTime { get; set; }
        [Column("DEPARTURE_TIME")] public DateTime? DepartureTime { get; set; }

        [Column("DECSTAT")] public int DecStat { get; set; }
        [Column("STOP_SEQUENCE")] public int StopSequence { get; set; }

        [Column("PICKUP_TYPE")] public int? PickupType { get; set; }

        [Column("SHAPE_DIST_TRAVELED")] public string? ShapeDistTraveled { get; set; }

        [Column("TIMEPOINT")] public int? Timepoint { get; set; }
        [Column("RT_ARRIVAL_TIME")] public DateTime? RtArrivalTime { get; set; }
        [Column("RT_DEPARTURE_TIME")] public DateTime? RtDepartureTime { get; set; }
        [Column("STATE")] public int? State { get; set; }
    }
}