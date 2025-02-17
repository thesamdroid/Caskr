using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public class ProductsService(IProductsRepository productsRepository)
    {
        public async Task<IEnumerable<Product?>> GetProductsAsync()
        {
            return await productsRepository.GetProductsAsync();
        }
        public async Task<Product?> GetProductAsync(int id)
        {
            return await productsRepository.GetProductAsync(id);
        }
        public async Task AddProductAsync(Product? product)
        {
            await productsRepository.AddProductAsync(product);
        }
        public async Task UpdateProductAsync(Product product)
        {
            await productsRepository.UpdateProductAsync(product);
        }
        public async Task DeleteProductAsync(int id)
        {
            await productsRepository.DeleteProductAsync(id);
        }
    }
}
