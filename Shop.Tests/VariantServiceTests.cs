using Moq;
using Xunit;
using Shop.Application.Services.Implementations;
using Shop.Application.Interfaces;
using Shop.Application.DTOs.Variants;
using Shop.Application.Repositories;
using Shop.Domain.Entities;
using Shop.Domain.Exceptions;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;

public class VariantServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnit;
    private readonly Mock<ICloudinaryService> _mockCloudinary;
    private readonly Mock<ISizeService> _mockSize;
    private readonly Mock<IColorService> _mockColor;
    private readonly VariantService _service;

    public VariantServiceTests()
    {
        _mockUnit = new Mock<IUnitOfWork>();
        _mockCloudinary = new Mock<ICloudinaryService>();
        _mockSize = new Mock<ISizeService>();
        _mockColor = new Mock<IColorService>();
        _service = new VariantService(_mockUnit.Object, _mockCloudinary.Object, _mockColor.Object, _mockSize.Object);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnVariantDto_WhenAddSuccess()
    {
        // Arrange
        var dto = new VariantAdd
        {
            ProductId = 1,
            ColorId = 1,
            SizeId = 1,
            Price = 100000,
            Quantity = 10,
            ImageFiles = new List<IFormFile>()
        };

        _mockUnit.Setup(x => x.VariantRepository.AddAsync(It.IsAny<Variant>()))
                 .Returns(Task.CompletedTask);
        _mockUnit.Setup(x => x.CompleteAsync()).ReturnsAsync(true);

        // Act
        var result = await _service.AddAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ProductId);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowNotFoundException_WhenVariantNotFound()
    {
        // Arrange
        _mockUnit.Setup(x => x.VariantRepository.GetAsync(It.IsAny<Expression<Func<Variant, bool>>>()))
                 .ReturnsAsync((Variant)null!);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(v => v.Id == 1));
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowNotFoundException_WhenVariantNotFound()
    {
        _mockUnit.Setup(x => x.VariantRepository.GetAsync(It.IsAny<Expression<Func<Variant, bool>>>()))
                 .ReturnsAsync((Variant)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(v => v.Id == 1));
    }
}
