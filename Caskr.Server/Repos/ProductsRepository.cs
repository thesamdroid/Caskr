using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IProductsRepository
    {
        Task<IEnumerable<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(int id);
        Task<Product> AddProductAsync(Product? product);
        Task<Product> UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
    }

    public class ProductsRepository(CaskrDbContext dbContext) : IProductsRepository
    {
        public async Task<IEnumerable<Product>> GetProductsAsync()
        {
            return (await dbContext.Products.ToListAsync())!;
        }
        public async Task<Product?> GetProductAsync(int id)
        {
            return await dbContext.Products.FindAsync(id);
        }
        public async Task<Product> AddProductAsync(Product? product)
        {
            var createdProduct = await dbContext.Products.AddAsync(product);
            await dbContext.SaveChangesAsync();
            return createdProduct.Entity!;
        }
        public async Task<Product> UpdateProductAsync(Product product)
        {
            dbContext.Entry(product).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
            return (await GetProductAsync(product.Id))!;
        }
        public async Task DeleteProductAsync(int id)
        {
            var product = await dbContext.Products.FindAsync(id);
            if (product != null)
            {
                dbContext.Products.Remove(product);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
