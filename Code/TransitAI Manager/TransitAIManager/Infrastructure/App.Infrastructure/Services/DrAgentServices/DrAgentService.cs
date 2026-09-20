using App.Infrastructure.DataAccessManager.EFCore.Contexts;
using Core.Application.Common.CQS.CqsModels.DrAgents.Queries;
using Core.Application.Common.CQS.Queries;
using Core.Application.Common.DtosModels.DrAgents;
using Core.Application.Common.Repositories;
using Core.Application.Services.DrAgentServices;
using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services.DrAgentServices
{
    public class DrAgentService:IDrAgentService
    {
        private readonly IQueryContext _queryContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly DataContext _context;

        public DrAgentService(
            IUnitOfWork unitOfWork,
            DataContext context, 
            IQueryContext queryable)
        {
            _unitOfWork = unitOfWork;
            //_commandClientRepo = commandClientRepo;
            _context = context;
            _queryContext = queryable;
        }
        public async Task<List<DrAgentDto>?> GetListDrAgentAsync(CancellationToken cancellationToken)
        {
            try
            {
                //var profileClients = await _queryContext.Set<ClientProfile>()
                //var agent = await _queryContext.Set<DrAgent>()
                
                //   // .OrderBy(o => o.Id )
                //    .ToListAsync(cancellationToken);

                var listAgentDto = await _queryContext.Set<DrAgent>()
                    .Join(
                        _queryContext.Set<DrDeleg>(),           // Table à joindre
                        agent => agent.DelegationId,            // Clé étrangère dans DrAgent
                        deleg => deleg.Id,                      // Clé primaire dans DrDeleg
                        (agent, deleg) => new DrAgentDto        // Projection
                        {
                            CodeAgent = agent.Id,
                            NomFr = agent.FullNameFr,
                            NomAr = agent.FullNameAr,
                            DelegationAr = deleg.NameAr,
                            DelegationFr = deleg.NameFr,
                        
                        }
                        )
                    .ToListAsync(cancellationToken);
                // var listAgentDto = agent.Select(ag => new DrAgentDto()
                // {
                //     CodeAgent = ag.Id,
                //     NomFr = ag.FullNameFr,
                //     NomAr = ag.FullNameAr,
                // }).ToList();

                return listAgentDto;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la récupération de liste des Agents : {ex.Message}", ex);
            }
        }
    }
}