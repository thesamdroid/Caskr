using Caskr.server.Models;
using Caskr.server.Repos;
using Caskr.server.Services;
using Moq;

namespace Caskr.Server.Tests;

public class ProductsServiceTests
{
    private readonly Mock<IProductsRepository> _repo = new();
    private readonly IProductsService _service;

    public ProductsServiceTests()
    {
        _service = new ProductsService(_repo.Object);
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsRepositoryResults()
    {
        var expected = new[] { new Product { Id = 1 } };
        _repo.Setup(r => r.GetProductsAsync()).ReturnsAsync(expected);

        var result = await _service.GetProductsAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetProductAsync_DelegatesToRepository()
    {
        var expected = new Product { Id = 2 };
        _repo.Setup(r => r.GetProductAsync(2)).ReturnsAsync(expected);

        var result = await _service.GetProductAsync(2);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetProductAsync_ReturnsNull_WhenNotFound()
    {
        _repo.Setup(r => r.GetProductAsync(9)).ReturnsAsync((Product?)null);

        var result = await _service.GetProductAsync(9);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddProductAsync_DelegatesToRepository()
    {
        var product = new Product { Id = 3 };
        _repo.Setup(r => r.AddProductAsync(product)).ReturnsAsync(product);

        var result = await _service.AddProductAsync(product);

        Assert.Equal(product, result);
    }

    [Fact]
    public async Task UpdateProductAsync_DelegatesToRepository()
    {
        var product = new Product { Id = 4 };
        _repo.Setup(r => r.UpdateProductAsync(product)).ReturnsAsync(product);

        var result = await _service.UpdateProductAsync(product);

        Assert.Equal(product, result);
    }

    [Fact]
    public async Task DeleteProductAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.DeleteProductAsync(5)).Returns(Task.CompletedTask);

        await _service.DeleteProductAsync(5);

        _repo.Verify(r => r.DeleteProductAsync(5), Times.Once);
    }
}
