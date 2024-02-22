namespace Mango.Web.Models
{
    public class CartDto
    {
        public CartHeaderDto CartHeader { get; set; }
        public IList<CartDetailsDto>? CartDetails { get; set; }
	}
}
