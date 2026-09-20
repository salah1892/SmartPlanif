using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CsvHelper.Configuration.Attributes;
namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// Personnel et Chauffeurs
    /// <para>Liste des agents</para>
    /// <para>Registre du Personnel Navigant et Administratif</para>
    /// </summary>

    // [Table("dragent")]
    // public class DrAgent
    // {
    //     [Key]
    //     [Name("DECAGEN")]
    //     public int Id { get; set; } // Matricule de l'agent
    //
    //     [Name("DENAGEN")]
    //     public string FullNameFr { get; set; }= string.Empty;
    //
    //     [Name("DENAGEA")]
    //     public string FullNameAr { get; set; }= string.Empty;
    //
    //     [Name("DECQUAL")]
    //     public string QualificationCode { get; set; } = string.Empty;// Chauffeur, Receveur, Contrôleur, Mécanicien
    //
    //     [Name("DECJOUR")]
    //     public int JournalCode { get; set; } // Type de roulement / horaire de base
    //
    //     [Name("DECCENT")]
    //     public int CenterId { get; set; } // Centre de rattachement principal
    //
    //     [Name("DECDELEG")]
    //     public int DelegationId { get; set; } // Lieu de résidence ou d'affectation
    //
    //     [Name("PDA")]
    //     public int? PdaNumber { get; set; } // ID du terminal billettique associé
    //     
    // }
    //
    public class DrAgent
    {
        //     [Key]
        //     [Name("DECAGEN")]
        public long Id { get; set; } // Matricule de l'agent

        [Column("DENAGEN")]
        public string? FullNameFr { get; set; }= string.Empty;

        [Column("DENAGEA")]
        public string? FullNameAr { get; set; }= string.Empty;

        [Column("DECQUAL")]
        public string? QualificationCode { get; set; } = string.Empty;// Chauffeur, Receveur, Contrôleur, Mécanicien

        [Column("DECJOUR")]
        public int? JournalCode { get; set; } // Type de roulement / horaire de base

        [Column("DECCENT")]
        public int? CenterId { get; set; } // Centre de rattachement principal

        [Column("DECDELEG")]
        public int? DelegationId { get; set; } // Lieu de résidence ou d'affectation

        [Column("PDA")]
        public int? PdaNumber { get; set; } // ID du terminal billettique associé
    
    }
}
