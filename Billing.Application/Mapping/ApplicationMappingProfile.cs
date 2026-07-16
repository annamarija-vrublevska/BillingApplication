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
        CreateMap<CreateOrderCommand, PaymentRequest>()
            .ForCtorParam(
                nameof(PaymentRequest.PaymentGatewayType),
                opt => opt.MapFrom(src => src.PaymentGatewayType))
            .ForCtorParam(
                nameof(PaymentRequest.Description),
                opt => opt.MapFrom(src => src.Description ?? string.Empty));
    }
}
