using AutoMapper;
using Core.Application.Common.CQS.CqsModels.DrAgents.Queries;
using Core.Application.Common.DtosModels.DrAgents;
using Core.Application.Common.DtosModels.DrLignes;
using Core.Application.Services.DrLignesServices;
using FluentValidation;
using MediatR;

namespace Core.Application.Common.CQS.CqsModels.DrLignes.Queries
{

    public class GetListDrLignesResult
    {
        public List<DrLigneDto>? Data { get; init; }
    }

    public class GetListDrLignesRequest : IRequest<GetListDrLignesResult>
    {
        //public string? IdClient { get; init; }
        //public string? CreatedById { get; set; }
    }

    public class GetDrLignesListValidator : AbstractValidator<GetListDrLignesRequest>
    {
        public GetDrLignesListValidator()
        {
        }
    }

    public class GetAllDrLignesQueryHandler : IRequestHandler<GetListDrLignesRequest, GetListDrLignesResult>
    {
        private readonly IDrLignesServices _lignesService;
        private readonly IMapper _mapper;
        public GetAllDrLignesQueryHandler(IDrLignesServices lignesServices, IMapper mapper)
        {
            _lignesService = lignesServices;
            _mapper = mapper;
        }
        public async Task<GetListDrLignesResult> Handle(GetListDrLignesRequest request, CancellationToken cancellationToken)
        {
            var listLignes = await _lignesService.GetListDrLigneAsync(cancellationToken);
            //var listAgentDto = _mapper.Map<List<DrAgentDto>>(listAgent);
            return new GetListDrLignesResult() { Data = listLignes };
        }
    }
}