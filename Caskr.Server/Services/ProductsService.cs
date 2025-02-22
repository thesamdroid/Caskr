using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public interface IProductsService
    {
        Task<IEnumerable<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(int id);
        Task<Product> AddProductAsync(Product? product);
        Task<Product> UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
    }

    public class ProductsService(IProductsRepository productsRepository) : IProductsService
    {
        public async Task<IEnumerable<Product>> GetProductsAsync()
        {
            return await productsRepository.GetProductsAsync();
        }
        public async Task<Product?> GetProductAsync(int id)
        {
            return await productsRepository.GetProductAsync(id);
        }
        public async Task<Product> AddProductAsync(Product? product)
        {
            return await productsRepository.AddProductAsync(product);
        }
        public async Task<Product> UpdateProductAsync(Product product)
        {
            return await productsRepository.UpdateProductAsync(product);
        }
        public async Task DeleteProductAsync(int id)
        {
            await productsRepository.DeleteProductAsync(id);
        }
    }
}
