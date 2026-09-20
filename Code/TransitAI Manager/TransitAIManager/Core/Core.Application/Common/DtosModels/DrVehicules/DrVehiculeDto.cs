using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Core.Application.Common.DtosModels.DrVehicules;

public class DrVehiculeDto
{

    /// <summary>
    /// Code véhicule (identifiant unique)
    /// </summary>
    public long Decodvh { get; set; }
    /// <summary>
    /// Matricule (plaque d'immatriculation)
    /// </summary>
    public string? Dematri { get; set; }

    /// <summary>
    /// Code catégorie véhicule (FK DRCATVE)
    /// </summary>
    public long? Decatvh { get; set; }

    /// <summary>
    /// Nombre de places assises
    /// </summary>
    public int? Denbrpa { get; set; }

    /// <summary>
    /// Nombre de places debout
    /// </summary>
    public int? Denbrpd { get; set; }

    /// <summary>
    /// Date de mise en circulation
    /// </summary>
    public DateTime? Dedatec { get; set; }

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
    public int? Deactif { get; set; }

    /// <summary>
    /// Nom Arabic catégorie véhicule
    /// </summary>
    public string? CategAr { get; set; }

    /// <summary>
    /// Nom Français catégorie véhicule
    /// </summary>
    public string? CategFr { get; set; }

}
