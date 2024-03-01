using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;
using Microsoft.AspNetCore.Authorization;
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

		[Authorize]
		public IActionResult OrderIndex()
		{
			return View();
		}

		[HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeaderDto> orders;

			string userId = "";
			//if (User.IsInRole(SD.RoleAdmin))
			//{
				userId = User.Claims.Where(w => w.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
			//}

			ResponseDto response = _orderService.GetAllOrder(userId).GetAwaiter().GetResult();

			if (response != null && response.IsSuccess)
			{
				orders = JsonConvert.DeserializeObject<List<OrderHeaderDto>>(Convert.ToString(response.Result));
				switch (status)
				{
					case "approved":
						orders = orders.Where(w => w.Status == OrderStatusSD.Approved).ToList();
						break;
					case "readyforpickup":
						orders = orders.Where(w => w.Status == OrderStatusSD.ReadyForPickup).ToList();
						break;
					case "cancelled":
						orders = orders.Where(w => w.Status == OrderStatusSD.Cancelled).ToList();
						break;
				}
			}
			else
			{
				orders = new List<OrderHeaderDto>();
			}

			return Json(new { data = orders });
		}

		[Authorize]
		public async Task<IActionResult> OrderDetail(int orderId)
		{
			OrderHeaderDto order = new OrderHeaderDto();
			string userId = User.Claims.Where(w => w.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;

			var response = await _orderService.GetOrder(orderId);

			if (response != null && response.IsSuccess)
			{
				order = JsonConvert.DeserializeObject<OrderHeaderDto>(Convert.ToString(response.Result));
			}

			if (!User.IsInRole(SD.RoleAdmin) && userId != order.UserId)
			{
				return NotFound();
			}

			return View(order);
		}

		[HttpPost("OrderReadyForPickup")]
		public async Task<IActionResult> OrderReadyForPickup(int orderId)
		{
			var response = await _orderService.UpdateOrderStatus(orderId, OrderStatusSD.ReadyForPickup);

			if (response != null && response.IsSuccess)
			{

				TempData["success"] = "Status updated successfuly";
				return RedirectToAction(nameof(OrderDetail), new { orderId });
			}

			return View();
		}

		[HttpPost("CompleteOrder")]
		public async Task<IActionResult> CompleteOrder(int orderId)
		{
			var response = await _orderService.UpdateOrderStatus(orderId, OrderStatusSD.Completed);

			if (response != null && response.IsSuccess)
			{

				TempData["success"] = "Status updated successfuly";
				return RedirectToAction(nameof(OrderDetail), new { orderId });
			}

			return View();
		}

		[HttpPost("CancelOrder")]
		public async Task<IActionResult> CancelOrder(int orderId)
		{
			var response = await _orderService.UpdateOrderStatus(orderId, OrderStatusSD.Cancelled);

			if (response != null && response.IsSuccess)
			{

				TempData["success"] = "Status updated successfuly";
				return RedirectToAction(nameof(OrderDetail), new { orderId });
			}

			return View();
		}
	}
}
