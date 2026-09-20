using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Metier
{
    //DrItin (pour les itinéraires / shapes)
    /// <summary>
    /// Itinéraires
    /// </summary>
    public class DrItin
    {
        // Clé composite configurée dans le DbContext
        [Column("denumli")] public int Denumli { get; set; }
        [Column("denumlg")] public int Denumlg { get; set; }
        [Column("decstat")] public int Decstat { get; set; }
        [Column("dekmsta")] public decimal? Dekmsta { get; set; }
        [Column("dedurtr")] public decimal? Dedurtr { get; set; }
        [Column("deescale")] public decimal? Deescale { get; set; }
        [Column("desecti")] public int? Desecti { get; set; }
    }
}