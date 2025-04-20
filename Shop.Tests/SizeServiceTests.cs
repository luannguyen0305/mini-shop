using Moq;
using Shop.Application.Services.Implementations;
using Shop.Application.Repositories;
using Shop.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class SizeServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ISizeRepository> _mockSizeRepository;
    private readonly SizeService _sizeService;

    public SizeServiceTests()
    {
        // Mock repository
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockSizeRepository = new Mock<ISizeRepository>();

        // Set up mock to return the mocked repository
        _mockUnitOfWork.Setup(u => u.SizeRepository).Returns(_mockSizeRepository.Object);

        // Initialize service with mocked unit of work
        _sizeService = new SizeService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task AddSize_ShouldThrowArgumentNullException_WhenSizeIsNull()
    {
        // Arrange
        Size size = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sizeService.AddSize(size));
    }

    [Fact]
    public async Task AddSize_ShouldThrowInvalidOperationException_WhenSizeAlreadyExists()
    {
        // Arrange
        var size = new Size { Name = "M" };
        _mockSizeRepository.Setup(r => r.SizeExistsAsync(size.Name)).ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sizeService.AddSize(size));
        Assert.Equal($"A size with name '{size.Name}' already exists.", exception.Message);
    }

    [Fact]
    public async Task AddSize_ShouldAddSizeSuccessfully_WhenValid()
    {
        // Arrange
        var size = new Size { Name = "L" };
        _mockSizeRepository.Setup(r => r.SizeExistsAsync(size.Name)).ReturnsAsync(false);
        _mockSizeRepository.Setup(r => r.AddAsync(size)).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(true);

        // Act
        await _sizeService.AddSize(size);

        // Assert
        _mockSizeRepository.Verify(r => r.AddAsync(size), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllSizesAsync_ShouldThrowKeyNotFoundException_WhenNoSizesExist()
    {
        // Arrange
        _mockSizeRepository.Setup(r => r.GetAllSizesAsync(true)).ReturnsAsync(new List<Size>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _sizeService.GetAllSizesAsync(true));
        Assert.Equal("No sizes found.", exception.Message);
    }

    [Fact]
    public async Task GetAllSizesAsync_ShouldReturnSizes_WhenSizesExist()
    {
        // Arrange
        var sizes = new List<Size> { new Size { Name = "M" }, new Size { Name = "L" } };
        _mockSizeRepository.Setup(r => r.GetAllSizesAsync(true)).ReturnsAsync(sizes);

        // Act
        var result = await _sizeService.GetAllSizesAsync(true);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, size => size.Name == "M");
        Assert.Contains(result, size => size.Name == "L");
    }

    [Fact]
    public async Task GetSizesById_ShouldThrowKeyNotFoundException_WhenSizeNotFound()
    {
        // Arrange
        _mockSizeRepository.Setup(r => r.GetSizesById(It.IsAny<int>())).ReturnsAsync((Size)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _sizeService.GetSizesById(1));
        Assert.Equal("Size with ID 1 not found.", exception.Message);
    }

    [Fact]
    public async Task GetSizesById_ShouldReturnSize_WhenSizeExists()
    {
        // Arrange
        var size = new Size { Id = 1, Name = "M" };
        _mockSizeRepository.Setup(r => r.GetSizesById(1)).ReturnsAsync(size);

        // Act
        var result = await _sizeService.GetSizesById(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("M", result.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowKeyNotFoundException_WhenSizeNotFound()
    {
        // Arrange
        var size = new Size { Id = 1, Name = "L" };
        _mockSizeRepository.Setup(r => r.GetSizesById(size.Id)).ReturnsAsync((Size)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _sizeService.DeleteAsync(size));
        Assert.Equal($"Size with ID {size.Id} not found.", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteSizeSuccessfully_WhenSizeExists()
    {
        // Arrange
        var size = new Size { Id = 1, Name = "L" };
        _mockSizeRepository.Setup(r => r.GetSizesById(size.Id)).ReturnsAsync(size);
        _mockSizeRepository.Setup(r => r.DeleteAsync(size)).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(true);

        // Act
        await _sizeService.DeleteAsync(size);

        // Assert
        _mockSizeRepository.Verify(r => r.DeleteAsync(size), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
