using AutoMapper;
using Core.Application.Common.CQS.CqsModels.DrLignes.Queries;
using Core.Application.Common.DtosModels.DrStation;
using Core.Application.Common.DtosModels.DrVehicules;
using Core.Application.Common.DtosModels.DrVehicules;
using Core.Application.Services.DrAgentServices;
using Core.Application.Services.DrStationServices;
using Core.Application.Services.DrVehiculeServices;
using FluentValidation;
using MediatR;

namespace Core.Application.Common.CQS.CqsModels.DrVehicules.Queries;

public class GetListDrVehiculesResult
{
    public List<DrVehiculeDto>? Data { get; init; }
}

public class GetListDrVehiculesRequest : IRequest<GetListDrVehiculesResult>
{

}

public class GetDrVehiculesListValidator : AbstractValidator<GetListDrVehiculesRequest>
{
    public GetDrVehiculesListValidator()
    {
    }
}

public class GetAllDrVehiculesQueryHandler : IRequestHandler<GetListDrVehiculesRequest, GetListDrVehiculesResult>
{
    private readonly IDrVehiculeService _vehiculeServices;
    private readonly IMapper _mapper;
    public GetAllDrVehiculesQueryHandler(IDrVehiculeService vehiculeServices, IMapper mapper)
    {
        _vehiculeServices = vehiculeServices;
        _mapper = mapper;
    }
    public async Task<GetListDrVehiculesResult> Handle(GetListDrVehiculesRequest request, CancellationToken cancellationToken)
    {
        var listVehicules = await _vehiculeServices.GetListDrVehiculeAsync(cancellationToken);
        //var listAgentDto = _mapper.Map<List<DrAgentDto>>(listAgent);
        return new GetListDrVehiculesResult() { Data = listVehicules };
    }
}