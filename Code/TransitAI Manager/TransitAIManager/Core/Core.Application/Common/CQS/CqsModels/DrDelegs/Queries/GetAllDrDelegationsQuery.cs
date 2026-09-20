

using AutoMapper;
using Core.Application.Common.DtosModels.DrAgents;
using Core.Application.Common.DtosModels.DrDelegation;
using Core.Application.Services.DrAgentServices;
using Core.Application.Services.DrDelegationServices;
using MediatR;
using FluentValidation;
namespace Core.Application.Common.CQS.CqsModels.DrDelegs.Queries
{
    public class GetListDrDelegationsResult
    {
        public List<DrDelegDto>? Data { get; init; }
    }

    public class GetListDelegationsRequest : IRequest<GetListDrDelegationsResult>
    {
        //public string? IdClient { get; init; }
        //public string? CreatedById { get; set; }
    }

    public class GetAgentsListValidator : AbstractValidator<GetListDelegationsRequest>
    {
        public GetAgentsListValidator()
        {
        }
    }

    public class GetAllDrAgentsQueryHandler : IRequestHandler<GetListDelegationsRequest, GetListDrDelegationsResult>
    {
        private readonly IDrDelegationService _delegService;
        private readonly IMapper _mapper;
        public GetAllDrAgentsQueryHandler(IDrDelegationService delegService, IMapper mapper)
        {
            _delegService = delegService;
            _mapper = mapper;
        }
        public async Task<GetListDrDelegationsResult> Handle(GetListDelegationsRequest request, CancellationToken cancellationToken)
        {
            var listDelegations = await _delegService.GetListDrDelegationAsync(cancellationToken);
            return new GetListDrDelegationsResult() { Data = listDelegations };
        }
    }
}