using Core.Application.Common.DtosModels.DrAgents;
using Core.Application.Common.DtosModels.DrLignes;
using Core.Application.Common.DtosModels.DrVehicules;
using Core.Domain.Entities.Metier;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Application.Common.DtosModels.DrDepart;

public class DrDepartAllDetailsDto
{

    /// <summary>
    /// 'ID séquence DrDepar'
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Numéro de départ
    /// </summary>
    public long? DENDEPA { get; set; }
    /// <summary>
    /// Code de service
    /// </summary>
    public long? DECSERV { get; set; }
    /// <summary>
    /// Numéro de ligne (FK DRLIGNE)
    /// </summary>
    public long? DENUMLI { get; set; }
    /// <summary>
    /// Code opération
    /// </summary>
    public long? DECOPER { get; set; }
    /// <summary>
    /// Séance (M=matin, S=soir)
    /// </summary>
    public string? DECSEAN { get; set; } = string.Empty;

    /// <summary>
    /// Date du départ
    /// </summary>
    // Les exports contiennent date + heure (même pour les champs "date").
    public DateTime? DEDATED { get; set; }
    /// <summary>
    /// Heure de fin de service
    /// </summary>
    public DateTime? DEHEUPS { get; set; }
    /// <summary>
    /// Heure de fin de service
    /// </summary>
    public DateTime? DEHEUFS { get; set; }
    /// <summary>
    /// Nombre de rotations
    /// </summary>
    public double? DENBRRO { get; set; }
    /// <summary>
    /// Heure d'arrivée
    /// </summary>
    public DateTime? DEHEUAA { get; set; }
    /// <summary>
    /// Heure de dernier retour
    /// </summary>
    public DateTime? DEHEUDR { get; set; }
    /// <summary>
    /// Heure de prise de service
    /// </summary>
    public DateTime? DEHEUPD { get; set; }
    /// <summary>
    /// Amplitude de la ligne
    /// </summary>
    public double? DEAMPLI { get; set; }
    /// <summary>
    /// Amplitude totale
    /// </summary>
    public double? DEAMPLT { get; set; }
    /// <summary>
    /// Code agent principal (FK DRAGENT)
    /// </summary>
    public long? DECAGEN { get; set; }
    /// <summary>
    /// Code agent secondaire
    /// </summary>
    public double? DECAGE1 { get; set; }
    /// <summary>
    /// Code véhicule (FK DRVEHIC)
    /// </summary>
    public long? DECODVH { get; set; }
    /// <summary>
    /// Kilomètres théoriques
    /// </summary>
    public double? DEKMTH { get; set; }
    /// <summary>
    /// Piste
    /// </summary>
    public string? DEPISTE { get; set; }
    /// <summary>
    /// Kilomètres chauffeur
    /// </summary>
    public double? DEKMCHA { get; set; }
    /// <summary>
    /// Kilomètres receveur
    /// </summary>
    public double? DEKMREC { get; set; }
    /// <summary>
    /// Code centre
    /// </summary>
    public long? DECCENT { get; set; }
    /// <summary>
    /// Observation (peut être en arabe)
    /// </summary>
    public string? DEOBSER { get; set; }
    /// <summary>
    /// Code additionnel 1
    /// </summary>
    public double? DECODE1 { get; set; }
    /// <summary>
    /// Code additionnel 2
    /// </summary>
    public double? DECODE2 { get; set; }
    /// <summary>
    /// Code additionnel 3 (peut contenir "V")
    /// </summary>
    public string? DECODE3 { get; set; }
    /// <summary>
    /// Type de comptage
    /// </summary>
    public string? DECTYPC { get; set; }
    /// <summary>
    /// Ordre de tri
    /// </summary>
    public double? DEORDTR { get; set; }
    /// <summary>
    /// État du départ
    /// </summary>
    public string? DEETAT { get; set; }
    /// <summary>
    /// Indicateur d'annulation
    /// </summary>
    public double? DEANNUL { get; set; }
    /// <summary>
    /// Date système
    /// </summary>
    public DateTime? DEDATES { get; set; }
    /// <summary>
    /// Origine des données
    /// </summary>
    public double? DEORIGD { get; set; }
    /// <summary>
    /// Indicateur de clôture
    /// </summary>
    public double? DECCLOT { get; set; }
    /// <summary>
    /// Numéro d'exercice (année)'
    /// </summary>
    public long? DECEXER { get; set; }
    /// <summary>
    /// Numéro du local
    /// </summary>
    public double? NUMLOC { get; set; }
    /// <summary>
    /// Code du Convention
    /// </summary>
    public double? CONV { get; set; }
    /// <summary>
    /// Code du Agent 1 prévu
    /// </summary>
    public double? PDECAGEN { get; set; }
    /// <summary>
    /// Code du Agent 2 prévu
    /// </summary>
    public double? PDECAGE1 { get; set; }
    /// <summary>
    /// Code du Véhicule prévu
    /// </summary>
    public double? PDECODVH { get; set; }
    /// <summary>
    /// Indicateur de suppression du voyage
    /// </summary>
    public double? VOY_SUP { get; set; }
    /// <summary>
    /// Indicateur de mise à jour du voyage
    /// </summary>
    public double? VOY_UPD { get; set; }
    /// <summary>
    /// Indicateur de modification du voyage
    /// </summary>
    public double? VOY_MOD { get; set; }
    /// <summary>
    /// Indicateur scolaire
    /// </summary>
    public double? SCOL { get; set; }
    /// <summary>
    /// SAE (système d'aide à l''exploitation)
    /// </summary>
    public double? SAE { get; set; }
    /// <summary>
    /// Code client
    /// </summary>
    public double? DECCLIE { get; set; }

    //---------------------------- Navigation Properties ---------------------------------//
    //public DrAgentDto? drAgent { get; set; }
    //public DrLigneDto? drLigne { get; set; }
    //public DrVehiculeDto? drVehic { get; set; }
    public DrAgent? drAgent { get; set; }
    public DrLigne? drLigne { get; set; }
    public DrVehic? drVehic { get; set; }

}
