using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// Catégories de Véhicules
    /// </summary>
    [Table("drcatve")]
    public class DrCatVe
    {
        /// <summary>
        /// Code catégorie véhicule (identifiant unique)
        /// </summary>
        [Key]
        [Column("DECATVH")] 
        public long Id { get; set; }

        /// <summary>
        /// Nom catégorie français (Ex: "BUS", "CAR", "VAN")
        /// </summary>
        [Column("DECATEG")] 
        public string Decateg { get; set; } = string.Empty;

        /// <summary>
        /// Nom catégorie arabic 
        /// </summary>
        [Column("DEACATE")] 
        public string Deacate { get; set; } = string.Empty;

        /// <summary>
        /// 'Nombre de places
        /// </summary>
        [Column("DENBPLC")] 
        public long? Capacity { get; set; } // Poids maximal autorisé
    }
}