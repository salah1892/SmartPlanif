using Core.Application.Common.DtosModels.DrDelegation;

namespace Core.Application.Services.DrDelegationServices
{
    public interface IDrDelegationService

    {

        /// <summary>
        /// Récupère  la liste des delegations 
        /// </summary>
        Task<List<DrDelegDto>?> GetListDrDelegationAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Récupère  la liste des delegations 
        /// </summary>
        //Task<List<DrDelegDto>?> GetListTypeDelegationAsync(CancellationToken cancellationToken);
    }
}
