using App.Infrastructure.DataAccessManager.EFCore.Contexts;
using Core.Application.Common.CQS.Queries;
using Core.Application.Common.DtosModels.DrAgents;
using Core.Application.Common.DtosModels.DrLignes;
using Core.Application.Common.Repositories;
using Core.Application.Services.DrLignesServices;
using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services.DrLignesServices
{
    public class DrLignesServices : IDrLignesServices
    {
        private readonly IQueryContext _queryContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly DataContext _context;

        //public DrLignesServices(IQueryContext queryContext, IUnitOfWork unitOfWork, DataContext context)
        //{
        //    _queryContext = queryContext;
        //    _unitOfWork = unitOfWork;
        //    _context = context;
        //}
        public DrLignesServices(IUnitOfWork unitOfWork, DataContext context)
        {
            //_queryContext = queryContext;
            _unitOfWork = unitOfWork;
            _context = context;

        }
        public async Task<List<DrLigneDto>?> GetListDrLigneAsync(CancellationToken cancellationToken)
        {
            try
            {
                //var profileClients = await _queryContext.Set<ClientProfile>()
                var lignes = await _context.Set<DrLigne>()
                
                    // .OrderBy(o => o.Id )
                    .ToListAsync(cancellationToken);

                var listLignesDto = lignes.Select(ag => new DrLigneDto()
                {
                    IdLigne = ag.Denumli,
                    NomligneFr = ag.Denomli,
                    NomligneAr = ag.Denomla,
                }).ToList();

                return listLignesDto;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la récupération de liste des Lignes : {ex.Message}", ex);
            }
        }
    }
}