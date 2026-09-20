using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// Types de lignes
    /// </summary>
    public class DrTyLi
    {
        [Key]
        [Column("dectyli")]
        public int Dectyli { get; set; }

        [Column("deltyli")] public string Deltyli { get; set; } = string.Empty;
        [Column("deltyla")] public string Deltyla { get; set; } = string.Empty;
    }
}