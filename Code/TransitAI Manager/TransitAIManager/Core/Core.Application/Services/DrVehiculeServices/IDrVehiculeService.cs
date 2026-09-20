using Core.Application.Common.DtosModels.   DrAgents;
using Core.Application.Common.DtosModels.DrVehicules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Application.Services.DrVehiculeServices
{
    public interface IDrVehiculeService
    {
        /// <summary>
        /// Récupère  la liste des Vehicules avec leur Ctégories 
        /// </summary>
        Task<List<DrVehiculeDto>?> GetListDrVehiculeAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Récupère  la liste des Vehicules avec leur Catégories 
        /// </summary>
        //Task<List<DrVehiculeDto>?> GetListDrVehiculeWithCategoriesAsync(CancellationToken cancellationToken);
    }
}
