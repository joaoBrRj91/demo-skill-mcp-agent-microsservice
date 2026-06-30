using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

namespace JL.Commerce.Tecnology.Service.Application.Mappings;

public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderPollingDto>()
            .ForMember(d => d.TransactionId, o => o.MapFrom(s => s.TransactionId))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.ErrorMessage, o => o.MapFrom(s => s.ErrorMessage))
            .ForMember(d => d.Order, o => o.MapFrom(s => s));

        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.Value))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.Items))
            .ForMember(d => d.PaymentMethod, o => o.MapFrom(s => s.Payment.Method.ToString()))
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(s => s.UpdatedAt));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.CatalogProductId, o => o.MapFrom(s => s.CatalogProductId))
            .ForMember(d => d.Quantity, o => o.MapFrom(s => s.Quantity))
            .ForMember(d => d.UnitPrice, o => o.MapFrom(s => s.UnitPrice));

        CreateMap<ShippingAddress, ShippingAddressDto>()
            .ForMember(d => d.City, o => o.MapFrom(s => s.City))
            .ForMember(d => d.State, o => o.MapFrom(s => s.State))
            .ForMember(d => d.ZipCode, o => o.MapFrom(s => s.ZipCode))
            .ForMember(d => d.Country, o => o.MapFrom(s => s.Country));
    }
}
