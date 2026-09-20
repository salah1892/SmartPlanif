using AutoMapper;
using Core.Application.Common.CQS.CqsModels.DrLignes.Queries;
using Core.Application.Common.DtosModels.DrStation;
using Core.Application.Services.DrAgentServices;
using Core.Application.Services.DrStationServices;
using FluentValidation;
using MediatR;

namespace Core.Application.Common.CQS.CqsModels.DrStations.Queries
{
    public class GetListDrStationsResult
    {
        public List<DrStationDto>? Data { get; init; }
    }

    public class GetListDrStationsRequest : IRequest<GetListDrStationsResult>
    {

    }

    public class GetDrStationsListValidator : AbstractValidator<GetListDrStationsRequest>
    {
        public GetDrStationsListValidator()
        {
        }
    }

    public class GetAllDrStationsQueryHandler : IRequestHandler<GetListDrStationsRequest, GetListDrStationsResult>
    {
        private readonly IDrStationServices _stationServices;
        private readonly IMapper _mapper;
        public GetAllDrStationsQueryHandler(IDrStationServices stationServices, IMapper mapper)
        {
            _stationServices = stationServices;
            _mapper = mapper;
        }
        public async Task<GetListDrStationsResult> Handle(GetListDrStationsRequest request, CancellationToken cancellationToken)
        {
            var listLignes = await _stationServices.GetListDrStationAsync(cancellationToken);
            //var listAgentDto = _mapper.Map<List<DrAgentDto>>(listAgent);
            return new GetListDrStationsResult() { Data = listLignes };
        }
    }
}