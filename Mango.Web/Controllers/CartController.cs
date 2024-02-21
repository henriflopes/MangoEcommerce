using Mango.Web.Models;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;

namespace Mango.Web.Controllers
{
	public class CartController : Controller
	{
		private readonly ICartService _cartService;

		public CartController(ICartService cartService)
		{
			_cartService = cartService;
		}

		[Authorize]
		public async Task<IActionResult> CartIndex()
		{
			var cart = await LoadCartDtoBasedOnLoggedInUser();

			if (cart.CartDetails.Count() == 0)
			{
				return RedirectToAction("Index", "Home");
			}

			return View(cart);
		}

		public async Task<IActionResult> Remove(int cartDetailsId)
		{
			var userId = User.Claims.Where(q => q.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
			ResponseDto? response = await _cartService.RemoveFromCartAsync(cartDetailsId);
			if (response != null & response.IsSuccess)
			{
				TempData["success"] = "Cart updated successfully";
				return RedirectToAction(nameof(CartIndex));
			}

			return View();
		}

		[HttpPost]
		public async Task<IActionResult> RemoveCoupon(CartDto cartDto)
		{
			cartDto.CartHeader.CouponCode = "";
			ResponseDto? response = await _cartService.ApplyCouponAsync(cartDto);
			if (response.Result != null & response.IsSuccess)
			{
				TempData["success"] = "The coupon has been applied successfully";
			}

			return RedirectToAction(nameof(CartIndex));
		}

		[HttpPost]
		public async Task<IActionResult> ApplyCoupon(CartDto cartDto)
		{
			var userId = User.Claims.Where(q => q.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
			ResponseDto? response = await _cartService.ApplyCouponAsync(cartDto);
			if (response.Result != null & response.IsSuccess)
			{
				TempData["success"] = "The coupon has been applied successfully";
			}
			else
			{
				TempData["error"] = "Invalid coupon";
			}

			return RedirectToAction(nameof(CartIndex));
		}

		private async Task<CartDto> LoadCartDtoBasedOnLoggedInUser()
		{
			var userId = User.Claims.Where(q => q.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
			ResponseDto? response = await _cartService.GetCartByUserAsync(userId);
			if (response != null & response.IsSuccess)
			{
				CartDto cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(response.Result));
				return cartDto;
			}

			return new CartDto();
		}
	}
}
