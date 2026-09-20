using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Core.Application.Common.DtosModels.DrVehicules
{
     class DrVehiculeCategoryDto
    {

        /// <summary>
        /// Code véhicule (identifiant unique)
        /// </summary>
        public int Decodvh { get; set; }
        /// <summary>
        /// Matricule (plaque d'immatriculation)
        /// </summary>
        public string? Dematri { get; set; }

        /// <summary>
        /// pour Code catégorie véhicule (FK DRCATVE)
        /// </summary>
        //------------------------------------//
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
        //------------------------------------//

        /// <summary>
        /// Nombre de places assises
        /// </summary>
        public int Denbrpa { get; set; }

        /// <summary>
        /// Nombre de places debout
        /// </summary>
        public int? Denbrpd { get; set; }

        /// <summary>
        /// Date de mise en circulation
        /// </summary>
        public DateTime Dedatec { get; set; }

        /// <summary>
        /// Code centre
        /// </summary>
        public double? Decent { get; set; }

        /// <summary>
        /// code délégation
        /// </summary>
        public double? Decdeleg { get; set; }

        /// <summary>
        /// Statut actif (1=actif, 0=inactif)
        /// </summary>
        public int Deactif { get; set; }

        /// <summary>
        /// Nom Arabic catégorie véhicule
        /// </summary>
        public string? CategAr { get; set; }

        /// <summary>
        /// Nom Français catégorie véhicule
        /// </summary>
        public string? CategFr { get; set; }
    }
}
