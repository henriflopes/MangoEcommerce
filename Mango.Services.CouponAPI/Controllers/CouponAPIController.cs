using AutoMapper;
using Mango.Services.CouponAPI.Data;
using Mango.Services.CouponAPI.Models;
using Mango.Services.CouponAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.CouponAPI.Controllers
{
	[Route("api/coupon")]
	[ApiController]
	[Authorize]
	public class CouponAPIController : ControllerBase
	{
		private readonly AppDbContext _context;
		private readonly IMapper _mapper;
		private readonly ResponseDto _response;

		public CouponAPIController(AppDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
			_response = new ResponseDto();
		}

		[HttpGet]
		public ResponseDto Get()
		{
			try
			{
				IEnumerable<Coupon> coupons = _context.Coupons.ToList();
				_response.Result = _mapper.Map<IEnumerable<CouponDto>>(coupons);
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}

		[HttpGet]
		[Route("{id:int}")]
		public ResponseDto Get(int id)
		{
			try
			{
				Coupon coupon = _context.Coupons.First(q => q.CouponId == id);
				_response.Result = _mapper.Map<CouponDto>(coupon);
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}

		[HttpGet]
		[Route("GetByCode/{code}")]
		public ResponseDto GetByCode(string code)
		{
			try
			{
				Coupon coupon = _context.Coupons.First(q => q.CouponCode.ToLower() == code.ToLower());

				if (coupon == null)
				{
					_response.IsSuccess = false;
				}

				_response.Result = _mapper.Map<CouponDto>(coupon);
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}

		[HttpPost]
		[Authorize( Roles = "ADMIN")]
		public ResponseDto Post([FromBody] CouponDto couponDto)
		{
			try
			{
				_context.Coupons.Add(_mapper.Map<Coupon>(couponDto));
				_context.SaveChanges();

				_response.Result = couponDto;
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}

		[HttpPut]
		[Authorize(Roles = "ADMIN")]
		public ResponseDto Put([FromBody] CouponDto couponDto)
		{
			try
			{
				_context.Coupons.Update(_mapper.Map<Coupon>(couponDto));
				_context.SaveChanges();

				_response.Result = couponDto;
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}


		[HttpDelete]
		[Route("{id:int}")]
		[Authorize(Roles = "ADMIN")]
		public ResponseDto Delete(int id)
		{
			try
			{
				Coupon coupon = _context.Coupons.First(q => q.CouponId == id);
				_context.Coupons.Remove(coupon);
				_context.SaveChanges();
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
