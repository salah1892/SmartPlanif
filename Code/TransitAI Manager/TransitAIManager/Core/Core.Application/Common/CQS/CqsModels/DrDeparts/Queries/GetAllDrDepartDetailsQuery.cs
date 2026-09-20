using AutoMapper;
using Core.Application.Common.DtosModels.DrAgents;
using Core.Application.Common.DtosModels.DrDepart;
using Core.Application.Services.DrAgentServices;
using Core.Application.Services.DrDepartServices;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Application.Common.CQS.CqsModels.DrDeparts.Queries
{

        public class GetAllDrDepartDetailsResult
    {
        public List<DrDepartAllDetailsDto>? Data { get; init; }
    }

    public class GetListDepartAllDetailsRequest : IRequest<GetAllDrDepartDetailsResult>
    {
        //public string? IdClient { get; init; }
        //public string? CreatedById { get; set; }
    }

    public class GetAllDrDepartDetailsListValidator : AbstractValidator<GetListDepartAllDetailsRequest>
    {
        public GetAllDrDepartDetailsListValidator()
        {
        }
    }

    public class GetAllDrDepartDetailsQueryHandler : IRequestHandler<GetListDepartAllDetailsRequest, GetAllDrDepartDetailsResult>
    {
        private readonly IDrDepartService _departService;
        private readonly IMapper _mapper;
        public GetAllDrDepartDetailsQueryHandler(IDrDepartService departService, IMapper mapper)
        {
            _departService = departService;
            _mapper = mapper;
        }
        public async Task<GetAllDrDepartDetailsResult> Handle(GetListDepartAllDetailsRequest request, CancellationToken cancellationToken)
        {
            var listDepart = await _departService.GetListDrDepartWithLigneAgentVehiculeDetailsAsync(cancellationToken);
            //var listDepartDto = _mapper.Map<List<DrDepartAllDetailsDto>>(listDepart);
            return new GetAllDrDepartDetailsResult() { Data = listDepart };
        }
    }
}