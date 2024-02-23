using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Service.IService;
using Mango.Services.OrderAPI.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace Mango.Services.OrderAPI.Controllers
{
	[Route("api/order")]
	[ApiController]
	public class OrderAPIController : ControllerBase
	{
		protected ResponseDto _response;
		private readonly AppDbContext _context;
		private readonly IProductService _productService;
		private readonly IMapper _mapper;

		public OrderAPIController(AppDbContext context, IProductService productService, IMapper mapper)
		{
			_context = context;
			_productService = productService;
			_mapper = mapper;
			_response = new ResponseDto();
		}

		[Authorize]
		[HttpPost("CreateOrder")]
		public async Task<ResponseDto> CreateOrder([FromBody] CartDto cartDto)
		{
			try
			{
				OrderHeaderDto orderHeaderDto = _mapper.Map<OrderHeaderDto>(cartDto.CartHeader);
				orderHeaderDto.OrderTime = DateTime.Now;
				orderHeaderDto.Status = SD.Status_Pending;
				orderHeaderDto.OrderDetails = _mapper.Map<IEnumerable<OrderDetailsDto>>(cartDto.CartDetails);

				OrderHeader orderCreated = _context.OrderHeaders.Add(_mapper.Map<OrderHeader>(orderHeaderDto)).Entity;
				await _context.SaveChangesAsync();

				orderHeaderDto.OrderHeaderId = orderCreated.OrderHeaderId;
				_response.Result = orderHeaderDto;
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}

		[Authorize]
		[HttpPost("CreateStripeSession")]
		public async Task<ResponseDto> CreateStripeSession([FromBody] StripeRequestDto stripeRequestDto)
		{
			try
			{
				var options = new SessionCreateOptions
				{
					SuccessUrl = stripeRequestDto.ApprovedUrl,
					CancelUrl = stripeRequestDto.CancelUrl,
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};

				foreach (var item in stripeRequestDto.OrderHeader.OrderDetails)
				{
					var sessionItemLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Price * 100), //$20.99 -> 2099
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Name

							}
						},
						Quantity = item.Count
					};

					options.LineItems.Add(sessionItemLineItem);
				}

				var service = new SessionService();
				Session session = service.Create(options);
				stripeRequestDto.StripeSessionUrl = session.Url;
				OrderHeader orderHeader = _context.OrderHeaders.First(q => q.OrderHeaderId == stripeRequestDto.OrderHeader.OrderHeaderId);
				orderHeader.StripeSessionId = session.Id;

				await _context.SaveChangesAsync();
				_response.Result = stripeRequestDto;
			}
			catch (Exception ex)
			{
				_response.Message = ex.Message;
				_response.IsSuccess = false;
			}

			return _response;
		}
	}
}
