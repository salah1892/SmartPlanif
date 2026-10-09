using Core.Application.Services.DrDelegationServices;
using Core.Application.Services.DrDepartServices;
using Core.Application.Services.DrVehiculeServices;
using Core.Application.Services.PlanningServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace App.Infrastructure.Services.Planning
{
    public class PlanningStaticOrchestratorService : IPlanningStaticOrchestratorService
    {
        private readonly IDrDelegationService _drDelegationService;
        private readonly IDrVehiculeService _drVehiculeService;
        private readonly IDrDepartService _drDepartService;


        public PlanningStaticOrchestratorService(
            IDrDelegationService drDelegationService,
            IDrVehiculeService drVehiculeService,
            IDrDepartService drDepartService
            )
        {
            _drDelegationService = drDelegationService;
            _drVehiculeService = drVehiculeService;
            _drDepartService = drDepartService;
        }
    }
}
