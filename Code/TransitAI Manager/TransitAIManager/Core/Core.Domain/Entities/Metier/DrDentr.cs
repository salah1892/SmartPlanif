using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;
namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// Dépôts / Centres de rattachement
    /// <para>Centre ou sont affecté les lignes</para>
    /// <para>Dépôts, Entreprises et Centres d'Exploitation</para>
    /// </summary>
    public class DrDentr
    {
        [Key]
        [Name("DECAGEN")]
        public int Id { get; set; } // Matricule de Dépôts

        [Name("DENAGEN")]
        public string FullNameFr { get; set; }= string.Empty;

        [Name("DENAGEA")]
        public string FullNameAr { get; set; }= string.Empty;

        [Name("DECQUAL")]
        public string QualificationCode { get; set; }= string.Empty; // Dépôts, Entreprises et Centres d'Exploitation

        [Name("DECJOUR")]
        public int JournalCode { get; set; } // Type de roulement / horaire de base

        [Name("DECCENT")]
        public int CenterId { get; set; } // Centre de rattachement principal

        [Name("DECDELEG")]
        public int DelegationId { get; set; } // Lieu de résidence ou d'affectation

        [Name("PDA")]
        public int? PdaNumber { get; set; } // ID du terminal billettique associé
    
    }
}