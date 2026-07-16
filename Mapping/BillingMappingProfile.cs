using AutoMapper;
using Billing.Api.Models;
using Billing.Application.Models;

namespace Billing.Api.Mapping;

/// <summary>
/// AutoMapper profile that maps Billing.Api transport models to Billing.Application models.
/// </summary>
public sealed class BillingMappingProfile : Profile
{
    public BillingMappingProfile()
    {
        // API request -> Application request.
        // PaymentGatewayId is used by the controller to resolve the gateway and has no
        // counterpart on PaymentRequest, so it is intentionally not mapped.
        CreateMap<CreateOrderRequest, PaymentRequest>()
            .ForCtorParam(
                nameof(PaymentRequest.Description),
                opt => opt.MapFrom(src => src.Description ?? string.Empty));

        // API request -> Application command (1:1 member match).
        CreateMap<CreateOrderRequest, CreateOrderCommand>();

        // Application result -> API response (1:1 member match).
        CreateMap<CreateOrderResult, PaymentReceiptResponse>();
    }
}
