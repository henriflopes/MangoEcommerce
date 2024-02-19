using Mango.Web.Models;

namespace Mango.Web.Service.IService
{
	public interface IProductService
	{
		Task<ResponseDto?> GetProductAsync(string name);
		Task<ResponseDto?> GetAllProductsAsync();
		Task<ResponseDto?> GetProductByIdAsync(int id);
		Task<ResponseDto?> CreateProductAsync(ProductDto productDto);
		Task<ResponseDto?> DeleteProductAsync(int id);
		Task<ResponseDto?> UpdateProductAsync(ProductDto productDto);
	}
}
