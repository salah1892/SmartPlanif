using App.Infrastructure.DataAccessManager.EFCore.Contexts;
using Core.Application.Common.CQS.Queries;
using Core.Application.Common.DtosModels.DrStation;
using Core.Application.Common.Repositories;
using Core.Application.Services.DrStationServices;
using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services.DrStation
{
    public class DrStationServices:IDrStationServices
    {
        private readonly IQueryContext _queryContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly DataContext _context;

        //public DrStationServices(IQueryContext queryContext, IUnitOfWork unitOfWork, DataContext context)
        //{
        //    _queryContext = queryContext;
        //    _unitOfWork = unitOfWork;
        //    _context = context;
        //}
        public DrStationServices(IUnitOfWork unitOfWork, DataContext context)
        {
           
            _unitOfWork = unitOfWork;
            _context = context;
        }
        public async Task<List<DrStationDto>?> GetListDrStationAsync(CancellationToken cancellationToken)
        {
            try
            {
                //var profileClients = await _queryContext.Set<ClientProfile>()
                //var statis = await _queryContext.Set<DrStati>().ToArrayAsync();
                var stations = await _context.Set<DrStati>().ToListAsync(cancellationToken);

                var listStationDto = stations.Select(ag => new DrStationDto()
                {
                     CodeStation= ag.DecStat,
                    NomStationFr = ag.DelStat,
                    NomStationAr = ag.DelStaA,
                    StopLatitude = ag.StopLat,
                    StopLongtitude = ag.StopLon,
                }).ToList();

                return listStationDto;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la récupération de liste des stations : {ex.Message}", ex);
            }
        }
    }
}