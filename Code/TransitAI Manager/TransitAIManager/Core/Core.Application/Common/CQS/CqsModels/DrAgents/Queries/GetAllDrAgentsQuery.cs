using AutoMapper;
using Core.Application.Services.DrAgentServices;
using MediatR;
using FluentValidation;
using Core.Application.Common.DtosModels.DrAgents;
namespace Core.Application.Common.CQS.CqsModels.DrAgents.Queries
{
    public class GetListDrAgentsResult
    {
        public List<DrAgentDto>? Data { get; init; }
    }

    public class GetListAgentsRequest : IRequest<GetListDrAgentsResult>
    {
        //public string? IdClient { get; init; }
        //public string? CreatedById { get; set; }
    }

    public class GetAgentsListValidator : AbstractValidator<GetListAgentsRequest>
    {
        public GetAgentsListValidator()
        {
        }
    }

    public class GetAllDrAgentsQueryHandler : IRequestHandler<GetListAgentsRequest, GetListDrAgentsResult>
    {
        private readonly IDrAgentService _agentService;
        private readonly IMapper _mapper;
        public GetAllDrAgentsQueryHandler(IDrAgentService agentService, IMapper mapper)
        {
            _agentService = agentService;
            _mapper = mapper;
        }
        public async Task<GetListDrAgentsResult> Handle(GetListAgentsRequest request, CancellationToken cancellationToken)
        {
            var listAgent = await _agentService.GetListDrAgentAsync(cancellationToken);
            //var listAgentDto = _mapper.Map<List<DrAgentDto>>(listAgent);
            return new GetListDrAgentsResult() { Data = listAgent };
        }
    }
}