using AutoMapper;
using Mango.MessageBus;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.RabbitMQSender;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection.PortableExecutable;

namespace Mango.Services.ShoppingCartAPI.Controllers
{
	[Route("api/cart")]
	[ApiController]
	public class CartAPIController : ControllerBase
	{
		private readonly AppDbContext _context;
		private readonly IMapper _mapper;
		private readonly IProductService _productService;
		private readonly ICouponService _couponService;
		private readonly IRabbitMQCartMessageSender _messageBus;
		private readonly IConfiguration _configuration;
		private readonly ResponseDto _response;

		public CartAPIController(AppDbContext context, IMapper mapper, IProductService productService, ICouponService couponService, IRabbitMQCartMessageSender messageBus, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_productService = productService;
			_couponService = couponService;
			_messageBus = messageBus;
			_configuration = configuration;
			_response = new ResponseDto();
		}

		[HttpGet]
		[Route("GetCart/{userId}")]
		public async Task<ResponseDto> GetCart(string userId)
		{
			try
			{
				CartDto cart = new()
				{
					CartHeader = _mapper.Map<CartHeaderDto>(_context.CartHeaders.First(q => q.UserId == userId))
				};

				cart.CartDetails = _mapper.Map<IEnumerable<CartDetailsDto>>(_context.CartDetails
					.Where(q => q.CartHeaderId == cart.CartHeader.CartHeaderId));

				IEnumerable<ProductDto> productDtos = await _productService.GetProducts();

				foreach (var item in cart.CartDetails)
				{
					item.Product = productDtos.FirstOrDefault(q => q.ProductId == item.ProductId);
					cart.CartHeader.CartTotal += (item.Count * item.Product.Price);
				}

				//apply coupon in any
				if (!string.IsNullOrEmpty(cart.CartHeader.CouponCode))
				{
					CouponDto coupon = await _couponService.GetCoupon(cart.CartHeader.CouponCode);
					if (coupon != null && cart.CartHeader.CartTotal > coupon.MinAmount)
					{
						cart.CartHeader.CartTotal -= coupon.DiscountAmount;
						cart.CartHeader.Discount = coupon.DiscountAmount;
					}
				}

				_response.Result = cart;

			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}

		[HttpPost("EmailCartRequest")]
		public async Task<ResponseDto> EmailCartRequest([FromBody] CartDto cartDto)
		{
			try
			{
				_messageBus.SendMessage(cartDto, _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue"));
				_response.Result = true;
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}

		[HttpPost("ApplyCoupon")]
		public async Task<ResponseDto> ApplyCoupon([FromBody] CartDto cartDto)
		{
			try
			{
				var coupon = await _couponService.GetCoupon(cartDto.CartHeader.CouponCode);

				if (!string.IsNullOrEmpty(coupon.CouponCode) || string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
				{
					var cartFromDb = await _context.CartHeaders.FirstAsync(q => q.UserId == cartDto.CartHeader.UserId);
					cartFromDb.CouponCode = cartDto.CartHeader.CouponCode;
					_context.CartHeaders.Update(cartFromDb);
					await _context.SaveChangesAsync();

					_response.Result = true;
				}
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}

		[HttpPost("CartUpsert")]
		public async Task<ResponseDto> CartUpsert(CartDto cartDto)
		{
			try
			{
				var cartHeaderFromDb = await _context.CartHeaders.FirstOrDefaultAsync(q =>
					q.UserId == cartDto.CartHeader.UserId
				);
				if (cartHeaderFromDb == null)
				{
					//create header and details
					CartHeader cartHeader = _mapper.Map<CartHeader>(cartDto.CartHeader);
					_context.CartHeaders.Add(cartHeader);
					await _context.SaveChangesAsync();

					cartDto.CartDetails.First().CartHeaderId = cartHeader.CartHeaderId;
					_context.CartDetails.Add(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
					await _context.SaveChangesAsync();
				}
				else
				{
					//if header is null
					//check if details has same product
					var cartDetailsFromDb = await _context.CartDetails.AsNoTracking().FirstOrDefaultAsync(q =>
						q.ProductId == cartDto.CartDetails.First().ProductId && q.CartHeaderId == cartHeaderFromDb.CartHeaderId
					);
					if (cartDetailsFromDb == null)
					{
						//create cartdetails
						cartDto.CartDetails.First().CartHeaderId = cartHeaderFromDb.CartHeaderId;
						_context.CartDetails.Add(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
						await _context.SaveChangesAsync();
					}
					else
					{
						//update count in cart details
						cartDto.CartDetails.First().Count += cartDetailsFromDb.Count;
						cartDto.CartDetails.First().CartHeaderId = cartDetailsFromDb.CartHeaderId;
						cartDto.CartDetails.First().CartDetailsId = cartDetailsFromDb.CartDetailsId;
						_context.CartDetails.Update(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
						await _context.SaveChangesAsync();
					}

					_response.Result = cartDto;
				}
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}

		[HttpPost("RemoveCart")]
		public async Task<ResponseDto> RemoveCart([FromBody] int cartDetailsId)
		{
			try
			{
				var cartDetails = _context.CartDetails
					.First(q => q.CartDetailsId == cartDetailsId);

				int totalCountOfCartItem = _context.CartDetails
					.Where(q => q.CartDetailsId == cartDetails.CartHeaderId).Count();
				_context.CartDetails.Remove(cartDetails);

				if (totalCountOfCartItem == 1)
				{
					var cartHeaderToRemove = await _context.CartHeaders
						.FirstOrDefaultAsync(q => q.CartHeaderId == cartDetails.CartHeaderId);
					_context.CartHeaders.Remove(cartHeaderToRemove);
				}
				await _context.SaveChangesAsync();

				_response.Result = true;
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}




	}
}
