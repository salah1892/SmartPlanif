using Core.Application.Common.CQS.CqsModels.DrAgents.Queries;
using Core.Application.Common.DtosModels.DrAgents;

namespace Core.Application.Services.DrAgentServices
{
    public interface IDrAgentService
    {

        /// <summary>
        /// Récupère  la liste des agents 
        /// </summary>
        Task<List<DrAgentDto>?> GetListDrAgentAsync(CancellationToken cancellationToken);  
    }
}