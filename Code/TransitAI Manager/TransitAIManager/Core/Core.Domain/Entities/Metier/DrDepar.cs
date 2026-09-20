using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// Points de depart
    /// </summary>
    public class DrDepar
    {    
        /// <summary>
        /// 'ID séquence DrDepar'
        /// </summary>
        [Key] [Column("Id")] public long Id { get; set; }
        /// <summary>
        /// Numéro de départ
        /// </summary>
        [Column("DENDEPA")] public long? DENDEPA { get; set; }
        /// <summary>
        /// Code de service
        /// </summary>
        [Column("DECSERV")] public long? DECSERV { get; set; }
        /// <summary>
        /// Numéro de ligne (FK DRLIGNE)
        /// </summary>
        [Column("DENUMLI")] public long? DENUMLI { get; set; }
        /// <summary>
        /// Code opération
        /// </summary>
        [Column("DECOPER")] public long? DECOPER { get; set; }
            /// <summary>
        /// Séance (M=matin, S=soir)
        /// </summary>
        [Column("DECSEAN")] public string? DECSEAN { get; set; } = string.Empty;

        /// <summary>
        /// Date du départ
        /// </summary>
        // Les exports contiennent date + heure (même pour les champs "date").
        [Column("DEDATED")] public DateTime? DEDATED { get; set; }
        /// <summary>
        /// Heure de fin de service
        /// </summary>
        [Column("DEHEUPS")] public DateTime? DEHEUPS { get; set; }
        /// <summary>
        /// Heure de fin de service
        /// </summary>
        [Column("DEHEUFS")] public DateTime? DEHEUFS { get; set; }
        /// <summary>
        /// Nombre de rotations
        /// </summary>
        [Column("DENBRRO")] public double? DENBRRO { get; set; }
        /// <summary>
        /// Heure d'arrivée
        /// </summary>
        [Column("DEHEUAA")] public DateTime? DEHEUAA { get; set; }
        /// <summary>
        /// Heure de dernier retour
        /// </summary>
        [Column("DEHEUDR")] public DateTime? DEHEUDR { get; set; }
        /// <summary>
        /// Heure de prise de service
        /// </summary>
        [Column("DEHEUPD")] public DateTime? DEHEUPD { get; set; }
        /// <summary>
        /// Amplitude de la ligne
        /// </summary>
        [Column("DEAMPLI")] public double? DEAMPLI { get; set; }
        /// <summary>
        /// Amplitude totale
        /// </summary>
        [Column("DEAMPLT")] public double? DEAMPLT { get; set; }
        /// <summary>
        /// Code agent principal (FK DRAGENT)
        /// </summary>
        [Column("DECAGEN")] public long? DECAGEN { get; set; }
        /// <summary>
        /// Code agent secondaire
        /// </summary>
        [Column("DECAGE1")] public double? DECAGE1 { get; set; }
        /// <summary>
        /// Code véhicule (FK DRVEHIC)
        /// </summary>
        [Column("DECODVH")] public long? DECODVH { get; set; }
        /// <summary>
        /// Kilomètres théoriques
        /// </summary>
        [Column("DEKMTH")] public double? DEKMTH { get; set; }
        /// <summary>
        /// Piste
        /// </summary>
        [Column("DEPISTE")] public string? DEPISTE { get; set; }
        /// <summary>
        /// Kilomètres chauffeur
        /// </summary>
        [Column("DEKMCHA")] public double? DEKMCHA { get; set; }
        /// <summary>
        /// Kilomètres receveur
        /// </summary>
        [Column("DEKMREC")] public double? DEKMREC { get; set; }
        /// <summary>
        /// Code centre
        /// </summary>
        [Column("DECCENT")] public long? DECCENT { get; set; }
        /// <summary>
        /// Observation (peut être en arabe)
        /// </summary>
        [Column("DEOBSER")] public string? DEOBSER { get; set; }
        /// <summary>
        /// Code additionnel 1
        /// </summary>
        [Column("DECODE1")] public double? DECODE1 { get; set; }
        /// <summary>
        /// Code additionnel 2
        /// </summary>
        [Column("DECODE2")] public double? DECODE2 { get; set; }
        /// <summary>
        /// Code additionnel 3 (peut contenir "V")
        /// </summary>
        [Column("DECODE3")] public string? DECODE3 { get; set; }
        /// <summary>
        /// Type de comptage
        /// </summary>
        [Column("DECTYPC")] public string? DECTYPC { get; set; }
        /// <summary>
        /// Ordre de tri
        /// </summary>
        [Column("DEORDTR")] public double? DEORDTR { get; set; }
        /// <summary>
        /// État du départ
        /// </summary>
        [Column("DEETAT")] public string? DEETAT { get; set; }
        /// <summary>
        /// Indicateur d'annulation
        /// </summary>
        [Column("DEANNUL")] public double? DEANNUL { get; set; }
        /// <summary>
        /// Date système
        /// </summary>
        [Column("DEDATES")] public DateTime? DEDATES { get; set; }
        /// <summary>
        /// Origine des données
        /// </summary>
        [Column("DEORIGD")] public double? DEORIGD { get; set; }
        /// <summary>
        /// Indicateur de clôture
        /// </summary>
        [Column("DECCLOT")] public double? DECCLOT { get; set; }
        /// <summary>
        /// Numéro d'exercice (année)'
        /// </summary>
        [Column("DECEXER")] public long? DECEXER { get; set; }
        /// <summary>
        /// Numéro du local
        /// </summary>
        [Column("NUMLOC")] public double? NUMLOC { get; set; }
        /// <summary>
        /// Code du Convention
        /// </summary>
        [Column("CONV")] public double? CONV { get; set; }
        /// <summary>
        /// Code du Agent 1 prévu
        /// </summary>
        [Column("PDECAGEN")] public double? PDECAGEN { get; set; }
        /// <summary>
        /// Code du Agent 2 prévu
        /// </summary>
        [Column("PDECAGE1")] public double? PDECAGE1 { get; set; }
        /// <summary>
        /// Code du Véhicule prévu
        /// </summary>
        [Column("PDECODVH")] public double? PDECODVH { get; set; }
        /// <summary>
        /// Indicateur de suppression du voyage
        /// </summary>
        [Column("VOY_SUP")] public double? VOY_SUP { get; set; }
        /// <summary>
        /// Indicateur de mise à jour du voyage
        /// </summary>
        [Column("VOY_UPD")] public double? VOY_UPD { get; set; }
        /// <summary>
        /// Indicateur de modification du voyage
        /// </summary>
        [Column("VOY_MOD")] public double? VOY_MOD { get; set; }
        /// <summary>
        /// Indicateur scolaire
        /// </summary>
        [Column("SCOL")] public double? SCOL { get; set; }
        /// <summary>
        /// SAE (système d'aide à l''exploitation)
        /// </summary>
        [Column("SAE")] public double? SAE { get; set; }
        /// <summary>
        /// Code client
        /// </summary>
        [Column("DECCLIE")] public double? DECCLIE { get; set; }

        //---------------------------- Navigation Properties ---------------------------------//

        //public DrAgent? drAgent { get; set; }
        //public DrLigne? drLigne { get; set; }
        //public DrVehic? drVehic { get; set; }
        [ForeignKey(nameof(DECAGEN))] // Tells EF that DECAGEN is the FK for drAgent
        public DrAgent? drAgent { get; set; }

        [ForeignKey(nameof(DENUMLI))] // Tells EF that DENUMLI is the FK for drLigne
        public DrLigne? drLigne { get; set; }

        [ForeignKey(nameof(DECODVH))] // Tells EF that DECODVH is the FK for drVehic
        public DrVehic? drVehic { get; set; }
    }
}