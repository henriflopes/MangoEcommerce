using AutoMapper;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;

namespace Mango.Services.OrderAPI
{
	public class MappingConfig
	{
		public static MapperConfiguration RegisterMaps()
		{
			var mappingConfig = new MapperConfiguration(config =>
			{
				config.CreateMap<OrderHeaderDto, CartHeaderDto>()
					.ForMember(dest => dest.CartTotal, q => q.MapFrom(src => src.OrderTotal)).ReverseMap();

                config.CreateMap<CartDetailsDto, OrderDetailsDto>()
					.ForMember(dest => dest.ProductName, q => q.MapFrom(src => src.Product.Name))
					.ForMember(dest => dest.Price, q => q.MapFrom(src => src.Product.Price))
					.ReverseMap();

				config.CreateMap<OrderHeaderDto, OrderHeader>().ReverseMap();
				config.CreateMap<OrderDetailsDto, OrderDetails>().ReverseMap();

			});

			return mappingConfig;
		}
	}
}
