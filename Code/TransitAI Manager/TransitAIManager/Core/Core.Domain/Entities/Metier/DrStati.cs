using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// les stations equivalent gtfs stops
    /// </summary>
    public class DrStati
    {
        /// <summary>
        /// Code station (identifiant unique)
        /// </summary>
        [Key]
        [Column("DECSTAT")]
        public int DecStat { get; set; }
        /// <summary>
        /// Nom en français
        /// </summary>
        [Column("DELSTAT")]
        public string DelStat { get; set; } = string.Empty;
        /// <summary>
        /// Nom en arabe
        /// </summary>
        [Column("DELSTAA")]
        public string DelStaA { get; set; } = string.Empty;
        /// <summary>
        /// Type de station
        /// </summary>
        [Column("DECTYST")]
        //public string DecTyst { get; set; } = string.Empty;
        public long DecTyst { get; set; } 
        /// <summary>
        /// Localisation
        /// </summary>
        [Column("DECLOCA")] 
        public string? DecLoca { get; set; }
        /// <summary>
        /// Route
        /// </summary>
        [Column("DECROUT")] 
        public string? DecRout { get; set; }
        /// <summary>
        /// Description de l''arrêt
        /// </summary>
        [Column("STOP_DESC")] 
        public string? StopDesc { get; set; }
        /// <summary>
        /// Latitude GPS (~35.6 pour Kairouan)
        /// </summary>
        [Column("STOP_LAT")] 
        public double? StopLat { get; set; }
        /// <summary>
        /// Longitude GPS (~10.0 pour Kairouan)
        /// </summary>
        [Column("STOP_LON")] 
        public double? StopLon { get; set; }
        /// <summary>
        /// Identifiant de zone
        /// </summary>
        [Column("ZONE_ID")] 
        public string? ZoneId { get; set; }
        /// <summary>
        /// URL d'information
        /// </summary>
        [Column("STOP_URL")] 
        public string? StopUrl { get; set; }
        /// <summary>
        /// Type de localisation GTFS
        /// </summary>
        [Column("LOCATION_TYPE")] 
        public int? LocationType { get; set; }
        /// <summary>
        /// Station parente
        /// </summary>
        [Column("PARENT_STATION")] 
        public int? ParentStation { get; set; }
        /// <summary>
        /// Fuseau horaire
        /// </summary>
        [Column("STOP_TIMEZONE")] 
        public string? StopTimezone { get; set; }
        /// <summary>
        /// Date système
        /// </summary>
        [Column("DATE_SYS")] 
        public DateTime? DateSys { get; set; } 
        /// <summary>
        /// Rayon en mètres (défaut 100)
        /// </summary>
        [Column("RAYON")] 
        public int Rayon { get; set; }
    }

}
