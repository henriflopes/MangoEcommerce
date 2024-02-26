using AutoMapper;
using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Models;
using Mango.Services.ProductAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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
		public ResponseDto Post(ProductDto productDto)
		{
			try
			{
				Product product = _mapper.Map<Product>(productDto);

				_context.Products.Add(product);
				_context.SaveChanges();

				if (productDto.Image != null)
				{
					string fileName = product.ProductId + Path.GetExtension(productDto.Image.FileName);
					string filePath = @"wwwroot\ProductImages\" + fileName;
					var filePatchDirectory = Path.Combine(Directory.GetCurrentDirectory(), filePath);
					using (var fileStream = new FileStream(filePatchDirectory, FileMode.Create)) 
					{
						productDto.Image.CopyTo(fileStream);
					}

					var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
					product.ImageUrl = $"{baseUrl}/ProductImages/{fileName}";
					product.ImageLocalPath = filePath;
				}
				else
				{
					product.ImageUrl = "https://placeholder.co/600x400";
				}
				_context.Products.Update(product);
				_context.SaveChanges();
				_response.Result = _mapper.Map<ProductDto>(productDto);
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
		public ResponseDto Put(ProductDto productDto)
		{
			try
			{
                Product product = _mapper.Map<Product>(productDto);

                if (productDto.Image != null)
                {
                    if (!string.IsNullOrEmpty(product.ImageLocalPath))
                    {
                        var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), product.ImageLocalPath);
                        FileInfo file = new FileInfo(oldFilePathDirectory);
                        if (file.Exists)
                        {
                            file.Delete();
                        }
                    }

                    string fileName = product.ProductId + Path.GetExtension(productDto.Image.FileName);
                    string filePath = @"wwwroot\ProductImages\" + fileName;
                    var filePatchDirectory = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                    using (var fileStream = new FileStream(filePatchDirectory, FileMode.Create))
                    {
                        productDto.Image.CopyTo(fileStream);
                    }

                    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                    product.ImageUrl = $"{baseUrl}/ProductImages/{fileName}";
                    product.ImageLocalPath = filePath;
                }

                _context.Products.Update(product);
				_context.SaveChanges();

				_response.Result = _mapper.Map<ProductDto>(product);
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
				if (!string.IsNullOrEmpty(product.ImageLocalPath))
				{
					var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), product.ImageLocalPath);
					FileInfo file = new FileInfo(oldFilePathDirectory);
					if (file.Exists)
					{
						file.Delete();
					}
				}
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
