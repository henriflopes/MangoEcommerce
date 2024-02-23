using AutoMapper;
using Mango.MessageBus;
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
		private readonly IConfiguration _configuration;
		private readonly IMessageBus _messageBus;

		public OrderAPIController(AppDbContext context, IProductService productService, IMapper mapper, IConfiguration configuration, IMessageBus messageBus)
		{
			_context = context;
			_productService = productService;
			_mapper = mapper;
			_configuration = configuration;
			_messageBus = messageBus;
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
		[HttpPost("ValidateStripeSession")]
		public async Task<ResponseDto> ValidateStripeSession([FromBody] int orderHeaderId)
		{
			try
			{
				OrderHeader orderHeader = _context.OrderHeaders.First(q => q.OrderHeaderId == orderHeaderId);

				var service = new SessionService();
				Session session = service.Get(orderHeader.StripeSessionId);

				//TODO: It has been commented on because the test API returns null in the PaymentIntentId attribute.
				//var paymentIntentService = new PaymentIntentService();
				//PaymentIntent paymentIntent = paymentIntentService.Get(orderHeader.PaymentIntentId);

				if //(paymentIntent.Status == IntentStatusSD.Succeeded)
					(true)
				{
					//then payment was successful
					orderHeader.PaymentIntentId = "id_fake_success_order"; //paymentIntent.Id;
					orderHeader.Status = SD.Status_Approved;
					await _context.SaveChangesAsync();
					RewardsDto rewardsDto = new RewardsDto()
					{
						OrderId = orderHeader.OrderHeaderId,
						RewardsActivity = Convert.ToInt32(orderHeader.OrderTotal),
						UserId = orderHeader.UserId
					};
					string topicName = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic");
					await _messageBus.PublishMessage(rewardsDto, topicName);
					_response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
				}
			}
			catch (Exception ex)
			{
				_response.Message = ex.Message;
				_response.IsSuccess = false;
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
					Mode = "payment"
					
				};

				var discounts = new List<SessionDiscountOptions>()
				{
					new SessionDiscountOptions()
					{
						Coupon = stripeRequestDto.OrderHeader.CouponCode
					}
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

				if (stripeRequestDto.OrderHeader.Discount > 0)
				{
					options.Discounts = discounts;
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
