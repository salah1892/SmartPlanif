using Core.Application.Common.DtosModels.DrStation;

namespace Core.Application.Services.DrStationServices
{
    public interface IDrStationServices
    {
        /// <summary>
        /// Récupère  la liste des Station-stop 
        /// </summary>
        Task<List<DrStationDto>?> GetListDrStationAsync(CancellationToken cancellationToken);
    }
}