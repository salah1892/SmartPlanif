using App.Infrastructure.DataAccessManager.EFCore.Contexts;
using Core.Application.Common.CQS.Queries;
using Core.Application.Common.DtosModels.DrDepart;
using Core.Application.Common.DtosModels.DrVehicules;
using Core.Application.Common.Repositories;
using Core.Application.Services.DrDepartServices;
using Core.Domain.Entities.Metier;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace App.Infrastructure.Services.DrDepartServices
{
    public class DrDepartService : IDrDepartService
    {
        private readonly IQueryContext _queryContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly DataContext _context;
        public DrDepartService(DataContext context, IUnitOfWork unitOfWork, IQueryContext queryContext)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _queryContext = queryContext;
        }
        public async Task<List<DrDepartAllDetailsDto>?> GetListDrDepartWithLigneAgentVehiculeDetailsAsync(CancellationToken cancellationToken)
        {
            try
            {
                
                var listDepartAllDetails = await _queryContext.Set<DrDepar>()
                .Include(depar => depar.drAgent)           // ← Utilise la navigation pour agent
                .Include(depar => depar.drLigne)           // ← Utilise la navigation pour ligne
                .Include(depar => depar.drVehic)      // ← Utilise la navigation pour vehicule
                .OrderBy(depar => depar.Id)
                .Select((depar) => new DrDepartAllDetailsDto
                {

                    Id = depar.Id,
                    DENDEPA = depar.DENDEPA,
                    DECSERV = depar.DECSERV,
                    DENUMLI = depar.DENUMLI,
                    DECOPER = depar.DECOPER,
                    DECSEAN = depar.DECSEAN,
                    DEDATED = depar.DEDATED,
                    DEHEUPS = depar.DEHEUPS,
                    DEHEUFS = depar.DEHEUFS,
                    DENBRRO = depar.DENBRRO,
                    DEHEUAA = depar.DEHEUAA,
                    DEHEUDR = depar.DEHEUDR,
                    DEHEUPD = depar.DEHEUPD,
                    DEAMPLI = depar.DEAMPLI,
                    DEAMPLT = depar.DEAMPLT,
                    DECAGEN = depar.DECAGEN,
                    DECAGE1 = depar.DECAGE1,
                    DECODVH = depar.DECODVH,
                    DEKMTH = depar.DEKMTH,
                    DEPISTE = depar.DEPISTE,
                    DEKMCHA = depar.DEKMCHA,
                    DEKMREC = depar.DEKMREC,
                    DECCENT = depar.DECCENT,
                    DEOBSER = depar.DEOBSER,
                    DECODE1 = depar.DECODE1,
                    DECODE2 = depar.DECODE2,
                    DECODE3 = depar.DECODE3,
                    DECTYPC = depar.DECTYPC,
                    DEORDTR = depar.DEORDTR,
                    DEETAT = depar.DEETAT,
                    DEANNUL = depar.DEANNUL,
                    DEDATES = depar.DEDATES,
                    DEORIGD = depar.DEORIGD,
                    DECCLOT = depar.DECCLOT,
                    DECEXER = depar.DECEXER,
                    NUMLOC = depar.NUMLOC,
                    CONV = depar.CONV,
                    PDECAGEN = depar.PDECAGEN,
                    PDECAGE1 = depar.PDECAGE1,
                    PDECODVH = depar.PDECODVH,
                    VOY_SUP = depar.VOY_SUP,
                    VOY_UPD = depar.VOY_UPD,
                    VOY_MOD = depar.VOY_MOD,
                    SCOL = depar.SCOL,
                    SAE = depar.SAE,
                    DECCLIE = depar.DECCLIE,

                    //---------------------------- Navigation Properties ---------------------------------//
                    drAgent = depar.drAgent,
                    drLigne = depar.drLigne,
                    drVehic = depar.drVehic
                })
                .ToListAsync(cancellationToken);
                return listDepartAllDetails;
            }
            catch (Exception ex)
            {

                throw new Exception($"Erreur lors de la récupération de liste des Départ avec tous les détails : {ex.Message}", ex);
            }
        }
    }
}
