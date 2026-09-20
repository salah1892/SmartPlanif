using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CsvHelper.Configuration.Attributes;

namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// Délégations / Zones Géographiques
    /// <para>Délégations et Découpage Administratif</para>
    /// </summary>
    public  class DrDeleg
    {
        [Key]
        [Column("DECDELEG")]
        public int Id { get; set; } // Code Délégation
    
        [Column("LIBDELEGFR")]
        public string NameFr { get; set; }= string.Empty;

        [Column("LIBDELEGAR")]
        public string NameAr { get; set; }= string.Empty;
    
        public ICollection<DrAgent> Agents { get; set; }  // Navigation inverse
    }
}
