using Core.Application.Common.DtosModels.DrLignes;

namespace Core.Application.Services.DrLignesServices
{
    public interface IDrLignesServices
    {
            /// <summary>
            /// Récupère  la liste des lignes 
            /// </summary>
            Task<List<DrLigneDto>?> GetListDrLigneAsync(CancellationToken cancellationToken);
    }
}