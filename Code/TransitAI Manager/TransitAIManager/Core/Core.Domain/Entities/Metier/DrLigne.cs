using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Metier
{
    /// <summary>
    /// ligne ou route selon gtfs
    /// <para>Infrastructure du Réseau / Lignes de Transport</para>
    /// </summary>
    /// 
    public class DrLigne
    {
        /// <summary>
        /// Numéro de ligne (identifiant unique)
        /// </summary>
        [Key]
        [Column("denumli")]
        public long? Denumli { get; set; }

        ///<summary>
        ///Nom en français
        /// </summary>
        [Column("denomli")]
        public string? Denomli { get; set; }

        ///<summary>
        ///Nom en arabe
        /// </summary>
        [Column("denomla")]
        public string? Denomla { get; set; }


        ///<summary>
        ///Code type de ligne (FK DRTYLI)
        /// </summary>
        [Column("dectyli")]
        public int? Dectyli { get; set; }
        ///<summary>
        ///Nombre de kilomètres
        /// </summary>
        [Column("denbrkm")]
        public double? Denbrkm { get; set; }

        ///<summary>
        ///Code centre
        /// </summary>
        [Column("deccent")]
        public int? Deccent { get; set; }
        ///<summary>
        ///Code délégation
        /// </summary>
        [Column("decdeleg")]
        public int? Decdeleg { get; set; }
        ///<summary>
        ///Nature de la ligne
        /// </summary>
        [Column("denatli")]
        public int ? Denatli { get; set; }
        ///<summary>
        ///Type de tarif
        /// </summary>
        [Column("dectyta")]
        public string? Dectyta { get; set; }
        /// <summary>
        /// Statut actif (1=actif, 0=inactif)
        /// </summary>
        [Column("deactif")]
        public int ? Deactif { get; set; }
        /// <summary>
        /// Intégration BI
        /// </summary>
        [Column("integ_bi")]
        public double ? IntegBi { get; set; }
        /// <summary>
        /// SIV
        /// </summary>
        [Column("siv")]
        public double ? Siv { get; set; }
        /// <summary>
        /// SAE (1=trips générés dans trips.csv)
        /// </summary>
        [Column("sae")]
        public double ? Sae { get; set; }

        public DrTyLi? Categorie { get; set; }   // ← Navigation property
    }
}