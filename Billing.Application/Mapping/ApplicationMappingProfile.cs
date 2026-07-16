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

        // Application payment result -> Application order result.
        CreateMap<PaymentResult, CreateOrderResult>()
            .ForCtorParam(
                nameof(CreateOrderResult.Status),
                opt => opt.MapFrom(_ => Billing.Domain.Models.OrderStatus.Paid))
            .ForCtorParam(
                nameof(CreateOrderResult.FailureReason),
                opt => opt.MapFrom(_ => (string?)null))
            .ForCtorParam(
                nameof(CreateOrderResult.IsIdempotentReplay),
                opt => opt.MapFrom(_ => false));
    }
}
