using Mango.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Web.Service.IService
{
    public interface IOrderService
    {
		Task<ResponseDto?> CreateOrder(CartDto cartDto);
		Task<ResponseDto?> CreateStripeSession(StripeRequestDto stripeRequestDto);
		Task<ResponseDto?> ValidateStripeSession(int orderHeaderId);
	}
}