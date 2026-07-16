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
        // API request -> Application command.
        CreateMap<CreateOrderRequest, CreateOrderCommand>()
            .ForCtorParam(
                nameof(CreateOrderCommand.PaymentGatewayType),
                opt => opt.MapFrom(src => src.PaymentGatewayType));

        // Application result -> API response (1:1 member match).
        CreateMap<CreateOrderResult, PaymentReceiptResponse>();
        CreateMap<GetOrderResult, OrderResponse>();
    }
}
