using Core.Application.Common.DtosModels.DrAgents;
using Core.Application.Common.DtosModels.DrDepart;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Application.Services.DrDepartServices
{
    public interface IDrDepartService
    {
        /// <summary>
        /// Récupère  la liste des agents 
        /// </summary>
        Task<List<DrDepartAllDetailsDto>?> GetListDrDepartWithLigneAgentVehiculeDetailsAsync(CancellationToken cancellationToken);
    }
}
