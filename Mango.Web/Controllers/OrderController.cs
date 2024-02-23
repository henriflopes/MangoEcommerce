using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;

namespace Mango.Web.Controllers
{
	public class OrderController : Controller
	{
		private readonly IOrderService _orderService;

		public OrderController(IOrderService orderService)
        {
			_orderService = orderService;
		}

        public IActionResult OrderIndex()
		{
			IEnumerable<OrderHeaderDto> orders;

			string userId = "";
			if (User.IsInRole(SD.RoleAdmin))
			{
				userId = User.Claims.Where(w => w.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
			}

			ResponseDto response = _orderService.GetAllOrder(userId).GetAwaiter().GetResult();

			if (response != null && response.IsSuccess)
			{
				orders = JsonConvert.DeserializeObject<List<OrderHeaderDto>>(Convert.ToString(response.Result));
			}
			else
			{
				orders = new List<OrderHeaderDto>();
			}

			return View(orders);
		}
		
		public async Task<IActionResult> OrderDetails(int orderId)
		{
			OrderHeaderDto order = new OrderHeaderDto();
			string userId = User.Claims.Where(w => w.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;

			var response = await _orderService.GetOrder(orderId);

			if (response != null && response.IsSuccess)
			{
				order = JsonConvert.DeserializeObject<OrderHeaderDto>(Convert.ToString(response.Result));
			}

			if(!User.IsInRole(SD.RoleAdmin) && userId != order.UserId)
			{
				return NotFound();
			}

			return View(order);
		}


	}
}
