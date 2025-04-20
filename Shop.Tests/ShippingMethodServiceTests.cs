using Moq;
using Shop.Application.Services.Implementations;
using Shop.Application.Repositories;
using Shop.Application.DTOs.ShippingMethods;
using Shop.Application.Mappers;
using Shop.Application.Exceptions;
using Shop.Domain.Entities;
using Shop.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Shop.Application.Tests
{
    public class ShippingMethodServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IShippingMethodRepository> _mockShippingMethodRepository;
        private readonly ShippingMethodService _shippingMethodService;

        public ShippingMethodServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockShippingMethodRepository = new Mock<IShippingMethodRepository>();
            _mockUnitOfWork.Setup(u => u.ShippingMethodRepository).Returns(_mockShippingMethodRepository.Object);
            _shippingMethodService = new ShippingMethodService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task AddAsync_ShouldThrowBadRequestException_WhenShippingMethodAlreadyExists()
        {
            // Arrange
            var shippingMethodAdd = new ShippingMethodAdd { Name = "Standard Shipping" };
            _mockShippingMethodRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()))
                                          .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(() => _shippingMethodService.AddAsync(shippingMethodAdd));
            Assert.Equal("Phương thức đã tồn tại", exception.Message);
        }

        [Fact]
        public async Task AddAsync_ShouldAddShippingMethodSuccessfully_WhenValid()
        {
            // Arrange
            var shippingMethodAdd = new ShippingMethodAdd { Name = "Express Shipping" };
            var shippingMethod = new ShippingMethod { Name = "Express Shipping" };

            _mockShippingMethodRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()))
                                          .ReturnsAsync(false);
            _mockShippingMethodRepository.Setup(r => r.AddAsync(It.IsAny<ShippingMethod>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(true);

            // Act
            var result = await _shippingMethodService.AddAsync(shippingMethodAdd);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Express Shipping", result.Name);
            _mockShippingMethodRepository.Verify(r => r.AddAsync(It.IsAny<ShippingMethod>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenShippingMethodNotFound()
        {
            // Arrange
            _mockShippingMethodRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()))
                                          .ReturnsAsync((ShippingMethod)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(() => _shippingMethodService.DeleteAsync(sm => sm.Id == 1));
            Assert.Equal("Phương thức không tồn tại", exception.Message);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteShippingMethodSuccessfully_WhenShippingMethodExists()
        {
            // Arrange
            var shippingMethod = new ShippingMethod { Id = 1, Name = "Standard Shipping" };
            _mockShippingMethodRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()))
                                          .ReturnsAsync(shippingMethod);
            _mockShippingMethodRepository.Setup(r => r.DeleteShippingMethodAsync(It.IsAny<ShippingMethod>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(true);

            // Act
            await _shippingMethodService.DeleteAsync(sm => sm.Id == 1);

            // Assert
            _mockShippingMethodRepository.Verify(r => r.DeleteShippingMethodAsync(It.IsAny<ShippingMethod>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldThrowKeyNotFoundException_WhenNoShippingMethodsExist()
        {
            // Arrange
            _mockShippingMethodRepository.Setup(r => r.GetAllShippingMethodAsync(It.IsAny<ShippingMethodParameters>(), true))
                                          .ReturnsAsync(new PagedList<ShippingMethod>(new List<ShippingMethod>(), 0, 1, 10));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _shippingMethodService.GetAllAsync(new ShippingMethodParameters(), true));
            Assert.Equal("No shipping methods found.", exception.Message);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnShippingMethods_WhenShippingMethodsExist()
        {
            // Arrange
            var shippingMethods = new List<ShippingMethod> { new ShippingMethod { Name = "Standard Shipping" }, new ShippingMethod { Name = "Express Shipping" } };
            var pagedList = new PagedList<ShippingMethod>(shippingMethods, 2, 1, 10);
            _mockShippingMethodRepository.Setup(r => r.GetAllShippingMethodAsync(It.IsAny<ShippingMethodParameters>(), true))
                                          .ReturnsAsync(pagedList);

            // Act
            var result = await _shippingMethodService.GetAllAsync(new ShippingMethodParameters(), true);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, sm => sm.Name == "Standard Shipping");
            Assert.Contains(result, sm => sm.Name == "Express Shipping");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenShippingMethodNotFound()
        {
            // Arrange
            var shippingMethodUpdate = new ShippingMethodUpdate { Id = 1, Name = "Express Shipping" };
            _mockShippingMethodRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()))
                                          .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(() => _shippingMethodService.UpdateAsync(shippingMethodUpdate));
            Assert.Equal("Phương thức không tồn tại", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBadRequestException_WhenShippingMethodNameAlreadyExists()
        {
            // Arrange
            var shippingMethodUpdate = new ShippingMethodUpdate { Id = 1, Name = "Express Shipping" };
            _mockShippingMethodRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()))
                                          .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(() => _shippingMethodService.UpdateAsync(shippingMethodUpdate));
            Assert.Equal("Phương thức đã tồn tại", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateShippingMethodSuccessfully_WhenValid()
        {
            // Arrange
            var shippingMethodUpdate = new ShippingMethodUpdate { Id = 1, Name = "Express Shipping" };
            var shippingMethod = new ShippingMethod { Id = 1, Name = "Express Shipping" };

            _mockShippingMethodRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()))
                                          .ReturnsAsync(true);
            _mockShippingMethodRepository.Setup(r => r.UpdateShippingMethodAsync(It.IsAny<ShippingMethod>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(true);

            // Act
            var result = await _shippingMethodService.UpdateAsync(shippingMethodUpdate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Express Shipping", result.Name);
            _mockShippingMethodRepository.Verify(r => r.UpdateShippingMethodAsync(It.IsAny<ShippingMethod>()), Times.Once);
        }
    }
}
