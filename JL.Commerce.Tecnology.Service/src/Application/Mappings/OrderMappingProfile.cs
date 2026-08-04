using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

namespace JL.Commerce.Tecnology.Service.Application.Mappings;

public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        // Simple value-object maps: all ctor param names match source property names exactly.
        CreateMap<ShippingAddress, ShippingAddressDto>();
        CreateMap<OrderItem, OrderItemDto>();

        // Order → OrderDto:
        //   ForCtorParam — resolves constructor parameters where source type differs from target type.
        //   ForMember    — resolves init-only property setters (AutoMapper 16 second pass for records).
        //   Both are required because AutoMapper 16 runs constructor resolution and property resolution
        //   as separate passes and does not share ForMember config with ForCtorParam (or vice-versa).
        CreateMap<Order, OrderDto>()
            .ForCtorParam("id", opt => opt.MapFrom(s => s.Id.Value))
            .ForCtorParam("paymentMethod", opt => opt.MapFrom(s => s.Payment.Method.ToString()))
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.Value))
            .ForMember(d => d.PaymentMethod, opt => opt.MapFrom(s => s.Payment.Method.ToString()));

        // Order → OrderPollingDto:
        //   ConstructUsing handles the conditional Order sub-object (null when Processing).
        //   ForMember(Status) prevents enum→string convention failure in the second pass.
        //   ForMember(Order, Ignore) prevents AutoMapper from overwriting the value set in ConstructUsing.
        CreateMap<Order, OrderPollingDto>()
            .ConstructUsing((s, ctx) => new OrderPollingDto(
                s.TransactionId,
                s.Status.ToString(),
                s.ErrorMessage,
                s.Status != OrderStatus.Processing ? ctx.Mapper.Map<OrderDto>(s) : null))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Order, opt => opt.Ignore());
    }
}
