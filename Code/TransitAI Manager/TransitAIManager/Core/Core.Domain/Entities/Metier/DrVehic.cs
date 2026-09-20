using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// liste des vehicules par categorie
    /// </summary>
    [Table("drvehic")]
    public class DrVehic
    {

        /// <summary>
        /// Code véhicule (identifiant unique)
        /// </summary>
        [Key]
    	[Column("DECODVH")]
    	///
        public long Decodvh { get; set; }
        /// <summary>
        /// Matricule (plaque d'immatriculation)
        /// </summary>
        [Column("DEMATRI")] 
        public string? Dematri { get; set; }

        /// <summary>
        /// Code catégorie véhicule (FK DRCATVE)
        /// </summary>
        [Column("DECATVH")] 
        public long? Decatvh { get; set; }
        [ForeignKey("Decatvh")]
        public DrCatVe? Categorie { get; set; }   // ← Navigation property

        /// <summary>
        /// Nombre de places assises
        /// </summary>
        [Column("DENBRPA")] 
        public int? Denbrpa { get; set; }

        /// <summary>
        /// Nombre de places debout
        /// </summary>
        [Column("DENBRPD")] 
        public int? Denbrpd { get; set; }

        /// <summary>
        /// Date de mise en circulation
        /// </summary>
        [Column("DEDATEC")] 
        public DateTime? Dedatec { get; set; }

        /// <summary>
        /// Code centre
        /// </summary>
        [Column("DECCENT")] 
        public double? Decent { get; set; }

        /// <summary>
        /// code délégation
        /// </summary>
        [Column("DECDELEG")] 
        public double? Decdeleg { get; set; }

        /// <summary>
        /// Statut actif (1=actif, 0=inactif)
        /// </summary>
        [Column("DEACTIF")] 
        public int? Deactif { get; set; } 
    }
}
