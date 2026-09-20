using App.Infrastructure.DataAccessManager.EFCore.Contexts;
using Core.Application.Common.CQS.Queries;
using Core.Application.Common.DtosModels.DrAgents;
using Core.Application.Common.DtosModels.DrVehicules;
using Core.Application.Common.Repositories;
using Core.Application.Services.DrVehiculeServices;
using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace App.Infrastructure.Services.DrVehiculeServices
{
    public class DrVehiculeService : IDrVehiculeService
    {

        private readonly IQueryContext _queryContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly DataContext _context;

        public DrVehiculeService(
            IUnitOfWork unitOfWork,
            DataContext context,
            IQueryContext queryable)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _queryContext = queryable;
        }
        public async Task<List<DrVehiculeDto>?> GetListDrVehiculeAsync(CancellationToken cancellationToken)
        {
            try
            {
                var vehicules = await _queryContext.Set<DrVehic>().ToListAsync(cancellationToken);

                var listVehicules = await _queryContext.Set<DrVehic>()
                    .Include(v => v.Categorie)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken); 

                var listVehiculesDto = await _queryContext.Set<DrVehic>()
                .Include(v => v.Categorie)           // ← Utilise la navigation
                .Select(v => new DrVehiculeDto
                {
                    Decodvh = v.Decodvh,
                    Dematri = v.Dematri,
                    Decatvh = v.Decatvh,
                    CategFr = v.Categorie != null ? v.Categorie.Decateg : null,
                    CategAr = v.Categorie != null ? v.Categorie.Deacate : null,
                    Denbrpa = v.Denbrpa,
                    Denbrpd = v.Denbrpd,
                    Dedatec = v.Dedatec,
                    Decent = v.Decent,
                    Decdeleg = v.Decdeleg,
                    Deactif = v.Deactif,
                })
                .ToListAsync(cancellationToken);


                return listVehiculesDto;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la récupération de liste des vehicules : {ex.Message}", ex);
            }
        }
    }
}
