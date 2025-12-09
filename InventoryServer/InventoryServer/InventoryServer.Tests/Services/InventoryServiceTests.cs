using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryServer.Models;
using InventoryServer.Repositories;
using InventoryServer.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace InventoryServer.Tests.Services
{
    public class InventoryServiceTests
    {
        private readonly Mock<IInventoryRepository> _mockRepository;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly InventoryService _service;
        private readonly List<InventoryItem> _testData;

        public InventoryServiceTests()
        {
            _mockRepository = new Mock<IInventoryRepository>();
            _mockConfiguration = new Mock<IConfiguration>();
            _service = new InventoryService(_mockRepository.Object, _mockConfiguration.Object);

            // Setup default configuration values
            _mockConfiguration.Setup(c => c.GetSection("FailureRate").Value).Returns("0.0");
            _mockConfiguration.Setup(c => c.GetSection("SimulatedDelay").Value).Returns("0");

            // Create test data
            _testData = new List<InventoryItem>
            {
                new InventoryItem
                {
                    PartNumber = "ABC-123",
                    SupplierSku = "SUP-001",
                    Description = "Widget A",
                    Branch = "NYC",
                    AvailableQty = 100,
                    Uom = "EA",
                    LeadTimeDays = 5,
                    LastPurchaseDate = DateTime.Now.AddDays(-10)
                },
                new InventoryItem
                {
                    PartNumber = "ABC-124",
                    SupplierSku = "SUP-002",
                    Description = "Widget B",
                    Branch = "LA",
                    AvailableQty = 50,
                    Uom = "EA",
                    LeadTimeDays = 3,
                    LastPurchaseDate = DateTime.Now.AddDays(-5)
                },
                new InventoryItem
                {
                    PartNumber = "ABC-123",
                    SupplierSku = "SUP-001",
                    Description = "Widget A",
                    Branch = "LA",
                    AvailableQty = 75,
                    Uom = "EA",
                    LeadTimeDays = 5,
                    LastPurchaseDate = DateTime.Now.AddDays(-8)
                },
                new InventoryItem
                {
                    PartNumber = "XYZ-789",
                    SupplierSku = "SUP-003",
                    Description = "Gadget C",
                    Branch = "NYC",
                    AvailableQty = 0,
                    Uom = "BOX",
                    LeadTimeDays = 10,
                    LastPurchaseDate = DateTime.Now.AddDays(-30)
                },
                new InventoryItem
                {
                    PartNumber = "DEF-456",
                    SupplierSku = "SUP-004",
                    Description = "Tool D",
                    Branch = "CHI",
                    AvailableQty = 200,
                    Uom = "EA",
                    LeadTimeDays = 7,
                    LastPurchaseDate = DateTime.Now.AddDays(-15)
                }
            };
        }

        #region SearchInventoryAsync Tests

        [Fact]
        public async Task SearchInventoryAsync_WithNoCriteria_ReturnsAllItems()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(5, result.Total);
            Assert.Equal(5, result.Items.Count);
        }

        [Fact]
        public async Task SearchInventoryAsync_WithPartNumberCriteria_FiltersCorrectly()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Criteria = "ABC",
                By = "partNumber",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(3, result.Total);
            Assert.All(result.Items, item => Assert.Contains("ABC", item.PartNumber, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SearchInventoryAsync_WithSupplierSkuCriteria_FiltersCorrectly()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Criteria = "SUP-001",
                By = "supplierSku",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(2, result.Total);
            Assert.All(result.Items, item => Assert.Equal("SUP-001", item.SupplierSku));
        }

        [Fact]
        public async Task SearchInventoryAsync_WithDescriptionCriteria_FiltersCorrectly()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Criteria = "Widget",
                By = "description",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(3, result.Total);
            Assert.All(result.Items, item => Assert.Contains("Widget", item.Description));
        }

        [Fact]
        public async Task SearchInventoryAsync_WithInvalidSearchField_DefaultsToPartNumber()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Criteria = "ABC",
                By = "invalidField",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(3, result.Total);
            Assert.All(result.Items, item => Assert.Contains("ABC", item.PartNumber));
        }

        [Fact]
        public async Task SearchInventoryAsync_WithBranchFilter_FiltersCorrectly()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Branches = "NYC,LA",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(4, result.Total);
            Assert.All(result.Items, item => Assert.True(item.Branch == "NYC" || item.Branch == "LA"));
        }

        [Fact]
        public async Task SearchInventoryAsync_WithOnlyAvailable_FiltersOutZeroQuantity()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                OnlyAvailable = true,
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(4, result.Total);
            Assert.All(result.Items, item => Assert.True(item.AvailableQty > 0));
        }

        [Fact]
        public async Task SearchInventoryAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Page = 1,
                Size = 2
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(5, result.Total);
            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task SearchInventoryAsync_WithSortAscending_SortsCorrectly()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Sort = "partNumber:asc",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal("ABC-123", result.Items.First().PartNumber);
        }

        [Fact]
        public async Task SearchInventoryAsync_WithSortDescending_SortsCorrectly()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Sort = "availableQty:desc",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(200, result.Items.First().AvailableQty);
        }

        [Fact]
        public async Task SearchInventoryAsync_WithMultipleFilters_AppliesAllFilters()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Criteria = "ABC-123",
                By = "partNumber",
                Branches = "LA",
                OnlyAvailable = true,
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(1, result.Total);
            Assert.Single(result.Items);
            Assert.Equal("ABC-123", result.Items.First().PartNumber);
            Assert.Equal("LA", result.Items.First().Branch);
            Assert.True(result.Items.First().AvailableQty > 0);
        }

        [Fact]
        public async Task SearchInventoryAsync_WithEmptyCriteria_IgnoresFilter()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Criteria = "",
                By = "partNumber",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(5, result.Total);
        }

        [Fact]
        public async Task SearchInventoryAsync_WithWhitespaceCriteria_IgnoresFilter()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Criteria = "   ",
                By = "partNumber",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(5, result.Total);
        }

        [Fact]
        public async Task SearchInventoryAsync_CaseInsensitiveSearch_FindsMatches()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Criteria = "abc",
                By = "partNumber",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(3, result.Total);
        }

        [Fact]
        public async Task SearchInventoryAsync_CaseInsensitiveBranches_FindsMatches()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Branches = "nyc,la",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(4, result.Total);
        }

        [Theory]
        [InlineData("partNumber")]
        [InlineData("supplierSku")]
        [InlineData("description")]
        [InlineData("branch")]
        [InlineData("availableQty")]
        [InlineData("uom")]
        [InlineData("leadTimeDays")]
        [InlineData("lastPurchaseDate")]
        public async Task SearchInventoryAsync_WithValidSortFields_SortsSuccessfully(string sortField)
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Sort = $"{sortField}:asc",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(5, result.Total);
            Assert.Equal(5, result.Items.Count);
        }

        [Fact]
        public async Task SearchInventoryAsync_WithInvalidSortField_UsesDefaultSort()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest
            {
                Sort = "invalidField:asc",
                Page = 0,
                Size = 10
            };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(5, result.Total);
            // Should default to partNumber sort
            Assert.Equal("ABC-123", result.Items.First().PartNumber);
        }

        #endregion

        #region GetPeakAvailabilityAsync Tests

        [Fact]
        public async Task GetPeakAvailabilityAsync_WithValidPartNumber_ReturnsCorrectTotals()
        {
            // Arrange
            var partNumber = "ABC-123";
            var items = _testData.Where(i => i.PartNumber == partNumber).ToList();
            _mockRepository.Setup(r => r.FindByPartNumberAsync(partNumber)).ReturnsAsync(items);

            // Act
            var result = await _service.GetPeakAvailabilityAsync(partNumber);

            // Assert
            Assert.Equal(partNumber, result.PartNumber);
            Assert.Equal(175, result.TotalAvailable); // 100 + 75
            Assert.Equal(2, result.Branches.Count);
        }

        [Fact]
        public async Task GetPeakAvailabilityAsync_OrdersBranchesByQuantityDescending()
        {
            // Arrange
            var partNumber = "ABC-123";
            var items = _testData.Where(i => i.PartNumber == partNumber).ToList();
            _mockRepository.Setup(r => r.FindByPartNumberAsync(partNumber)).ReturnsAsync(items);

            // Act
            var result = await _service.GetPeakAvailabilityAsync(partNumber);

            // Assert
            Assert.Equal("NYC", result.Branches.First().Branch); // 100 is highest
            Assert.Equal(100, result.Branches.First().Qty);
            Assert.Equal("LA", result.Branches.Last().Branch); // 75 is second
            Assert.Equal(75, result.Branches.Last().Qty);
        }

        [Fact]
        public async Task GetPeakAvailabilityAsync_WithNoItems_ReturnsZeroTotal()
        {
            // Arrange
            var partNumber = "NONEXISTENT";
            _mockRepository.Setup(r => r.FindByPartNumberAsync(partNumber)).ReturnsAsync(new List<InventoryItem>());

            // Act
            var result = await _service.GetPeakAvailabilityAsync(partNumber);

            // Assert
            Assert.Equal(partNumber, result.PartNumber);
            Assert.Equal(0, result.TotalAvailable);
            Assert.Empty(result.Branches);
        }

        [Fact]
        public async Task GetPeakAvailabilityAsync_WithSingleBranch_ReturnsCorrectData()
        {
            // Arrange
            var partNumber = "DEF-456";
            var items = _testData.Where(i => i.PartNumber == partNumber).ToList();
            _mockRepository.Setup(r => r.FindByPartNumberAsync(partNumber)).ReturnsAsync(items);

            // Act
            var result = await _service.GetPeakAvailabilityAsync(partNumber);

            // Assert
            Assert.Equal(partNumber, result.PartNumber);
            Assert.Equal(200, result.TotalAvailable);
            Assert.Single(result.Branches);
            Assert.Equal("CHI", result.Branches.First().Branch);
        }

        [Fact]
        public async Task GetPeakAvailabilityAsync_WithZeroQuantity_ReturnsCorrectData()
        {
            // Arrange
            var partNumber = "XYZ-789";
            var items = _testData.Where(i => i.PartNumber == partNumber).ToList();
            _mockRepository.Setup(r => r.FindByPartNumberAsync(partNumber)).ReturnsAsync(items);

            // Act
            var result = await _service.GetPeakAvailabilityAsync(partNumber);

            // Assert
            Assert.Equal(partNumber, result.PartNumber);
            Assert.Equal(0, result.TotalAvailable);
            Assert.Single(result.Branches);
            Assert.Equal(0, result.Branches.First().Qty);
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public async Task SearchInventoryAsync_WithSimulatedDelay_TakesExpectedTime()
        {
            // Arrange
            var delayMs = 100;
            _mockConfiguration.Setup(c => c.GetSection("SimulatedDelay").Value).Returns(delayMs.ToString());
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest { Page = 0, Size = 10 };

            var startTime = DateTime.Now;

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            Assert.True(elapsed >= delayMs, $"Expected at least {delayMs}ms delay, but took {elapsed}ms");
        }

        [Fact]
        public async Task SearchInventoryAsync_WithFailureRate_ThrowsException()
        {
            // Arrange
            _mockConfiguration.Setup(c => c.GetSection("FailureRate").Value).Returns("1.0");
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest { Page = 0, Size = 10 };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.SearchInventoryAsync(request)
            );
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task SearchInventoryAsync_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync())
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));
            var request = new InventorySearchRequest { Page = 0, Size = 10 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.SearchInventoryAsync(request)
            );
            Assert.Equal("Database connection failed", exception.Message);
        }

        [Fact]
        public async Task GetPeakAvailabilityAsync_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var partNumber = "ABC-123";
            _mockRepository.Setup(r => r.FindByPartNumberAsync(partNumber))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetPeakAvailabilityAsync(partNumber)
            );
            Assert.Equal("Database connection failed", exception.Message);
        }

        [Fact]
        public async Task SearchInventoryAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _service.SearchInventoryAsync(null)
            );
        }

        [Fact]
        public async Task GetPeakAvailabilityAsync_WithNullPartNumber_HandlesGracefully()
        {
            // Arrange
            _mockRepository.Setup(r => r.FindByPartNumberAsync(null))
                .ReturnsAsync(new List<InventoryItem>());

            // Act
            var result = await _service.GetPeakAvailabilityAsync(null);

            // Assert
            Assert.Null(result.PartNumber);
            Assert.Equal(0, result.TotalAvailable);
            Assert.Empty(result.Branches);
        }

        [Fact]
        public async Task SearchInventoryAsync_WithNegativePage_HandlesGracefully()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest { Page = -1, Size = 10 };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert - LINQ Skip with negative value treats it as 0
            Assert.Equal(5, result.Total);
            Assert.NotEmpty(result.Items);
        }

        [Fact]
        public async Task SearchInventoryAsync_WithNegativeSize_ReturnsEmptyItems()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest { Page = 0, Size = -1 };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(5, result.Total); // Total is still calculated
            Assert.Empty(result.Items); // But no items are returned
        }

        [Fact]
        public async Task SearchInventoryAsync_WithZeroSize_ReturnsEmptyItems()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest { Page = 0, Size = 0 };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(5, result.Total);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task SearchInventoryAsync_WithVeryLargePage_ReturnsEmptyItems()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllItemsAsync()).ReturnsAsync(_testData);
            var request = new InventorySearchRequest { Page = 1000, Size = 10 };

            // Act
            var result = await _service.SearchInventoryAsync(request);

            // Assert
            Assert.Equal(5, result.Total);
            Assert.Empty(result.Items); // No items on page 1000
        }

        #endregion
    }
}
