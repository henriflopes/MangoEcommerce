using AutoMapper;
using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Models;
using Mango.Services.ProductAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.ProductAPI.Controllers
{

	[Route("api/product")]
	[ApiController]
	public class ProductAPIController : ControllerBase
	{

		private readonly AppDbContext _context;
		private readonly IMapper _mapper;
		private readonly ResponseDto _response;

		public ProductAPIController(AppDbContext context, IMapper mapper)
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
				IEnumerable<Product> products = _context.Products.ToList();
				_response.Result = _mapper.Map<IEnumerable<ProductDto>>(products);
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
				Product product = _context.Products.First(q => q.ProductId == id);
				_response.Result = _mapper.Map<ProductDto>(product);
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}

		[HttpGet]
		[Route("GetByName/{name}")]
		public ResponseDto GetByName(string name)
		{
			try
			{
				List<Product> products = _context.Products.Where(q => q.Name.ToLower().Contains(name.ToLower())).ToList();

				if (products == null)
				{
					_response.IsSuccess = false;
				}

				_response.Result = _mapper.Map< List<ProductDto>>(products);
			}
			catch (Exception ex)
			{
				_response.IsSuccess = false;
				_response.Message = ex.Message;
			}

			return _response;
		}

		[HttpPost]
		[Authorize(Roles = "ADMIN")]
		public ResponseDto Post([FromBody] ProductDto productDto)
		{
			try
			{
				_context.Products.Add(_mapper.Map<Product>(productDto));
				_context.SaveChanges();

				_response.Result = productDto;
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
		public ResponseDto Put([FromBody] ProductDto productDto)
		{
			try
			{
				_context.Products.Update(_mapper.Map<Product>(productDto));
				_context.SaveChanges();

				_response.Result = productDto;
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
				Product product = _context.Products.First(q => q.ProductId == id);
				_context.Products.Remove(product);
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
