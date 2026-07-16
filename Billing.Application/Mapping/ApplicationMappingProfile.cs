using AutoMapper;
using Billing.Application.Models;

namespace Billing.Application.Mapping;

/// <summary>
/// AutoMapper profile for mappings between Billing.Application models.
/// </summary>
public sealed class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        // Application command -> Application payment request.
        // PaymentGatewayId has no counterpart on PaymentRequest and is not mapped.
        CreateMap<CreateOrderCommand, PaymentRequest>()
            .ForCtorParam(
                nameof(PaymentRequest.Description),
                opt => opt.MapFrom(src => src.Description ?? string.Empty));

        // Application payment result -> Application order result (1:1 member match).
        CreateMap<PaymentResult, CreateOrderResult>();
    }
}
