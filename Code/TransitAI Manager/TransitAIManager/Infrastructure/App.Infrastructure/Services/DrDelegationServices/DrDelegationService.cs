using App.Infrastructure.DataAccessManager.EFCore.Contexts;
using Core.Application.Common.CQS.CqsModels.DrAgents.Queries;
using Core.Application.Common.CQS.Queries;
using Core.Application.Common.DtosModels.DrAgents;
using Core.Application.Common.DtosModels.DrDelegation;
using Core.Application.Common.Repositories;
using Core.Application.Services.DrAgentServices;
using Core.Application.Services.DrDelegationServices;
using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services.DrDelegationServices
{
    public class DrDelegationService: IDrDelegationService
    {
        private readonly IQueryContext _queryContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly DataContext _context;

        public DrDelegationService(
            IUnitOfWork unitOfWork,
            DataContext context, 
            IQueryContext queryable)
        {
            _unitOfWork = unitOfWork;
            //_commandClientRepo = commandClientRepo;
            _context = context;
            _queryContext = queryable;
        }
        public async Task<List<DrDelegDto>?> GetListDrDelegationAsync(CancellationToken cancellationToken)
        {
            try
            {
                //var profileClients = await _queryContext.Set<ClientProfile>()
                var delegations = await _queryContext.Set<DrDeleg>().ToListAsync(cancellationToken);

                var listdelegations = delegations.Select(ag => new DrDelegDto()
                {
                    IdDelegation = ag.Id,
                    NameDelegAr = ag.NameAr,
                    NameDelegFr = ag.NameFr,
                
                }).ToList();

                return listdelegations;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la récupération de liste des Délégations : {ex.Message}", ex);
            }
        }
    }
}