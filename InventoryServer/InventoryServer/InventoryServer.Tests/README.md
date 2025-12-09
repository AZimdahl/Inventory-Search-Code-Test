# InventoryServer.Tests

Comprehensive unit tests for the InventoryServer service layer.

## Test Coverage

### InventoryServiceTests

This test suite provides complete coverage for the `InventoryService` class with 39 tests covering:

#### Search Functionality Tests (22 tests)
- **Basic Search**: Tests searching with no criteria, returning all items
- **Field-specific Search**: Tests searching by `partNumber`, `supplierSku`, and `description`
- **Invalid Field Handling**: Tests that invalid search fields default to `partNumber`
- **Branch Filtering**: Tests filtering by one or more branches (comma-separated)
- **Availability Filtering**: Tests filtering for only available items (qty > 0)
- **Pagination**: Tests page/size functionality for result sets
- **Sorting**: Tests all sortable fields (partNumber, supplierSku, description, branch, availableQty, uom, leadTimeDays, lastPurchaseDate) in both ascending and descending order
- **Invalid Sort Handling**: Tests that invalid sort fields use default sorting
- **Multiple Filter Combination**: Tests applying search criteria, branch filter, and availability filter simultaneously
- **Empty/Whitespace Criteria**: Tests that empty or whitespace criteria are ignored
- **Case Insensitivity**: Tests case-insensitive searching for both criteria and branches

#### Peak Availability Tests (5 tests)
- **Valid Part Number**: Tests aggregating availability across branches
- **Branch Ordering**: Tests that branches are ordered by quantity (descending)
- **No Items**: Tests behavior when part number doesn't exist
- **Single Branch**: Tests when item exists in only one branch
- **Zero Quantity**: Tests handling of items with zero availability

#### Configuration Tests (2 tests)
- **Simulated Delay**: Tests that configured delays are properly applied
- **Failure Rate**: Tests that simulated failures throw expected exceptions

#### Error Handling Tests (9 tests)
- **Repository Exceptions**: Tests that repository errors propagate correctly for both search and availability methods
- **Null Request**: Tests handling of null search requests
- **Null Part Number**: Tests handling of null part numbers in availability queries
- **Negative Page**: Tests behavior with negative page numbers
- **Negative Size**: Tests behavior with negative page sizes
- **Zero Size**: Tests behavior with zero page size
- **Out of Range Page**: Tests behavior when requesting pages beyond available data

## Dependencies

- **xUnit**: Testing framework
- **Moq**: Mocking library for dependencies
- **.NET 8.0**: Target framework

## Running Tests

Run all tests:
```powershell
dotnet test
```

Run with verbose output:
```powershell
dotnet test --verbosity normal
```

Run specific test:
```powershell
dotnet test --filter "FullyQualifiedName~SearchInventoryAsync_WithNoCriteria_ReturnsAllItems"
```

## Test Structure

Each test follows the **Arrange-Act-Assert** pattern:

1. **Arrange**: Set up mock dependencies and test data
2. **Act**: Execute the method being tested
3. **Assert**: Verify the expected outcome

## Test Data

The tests use a consistent set of test data including:
- 5 inventory items
- Multiple branches (NYC, LA, CHI)
- Various part numbers (ABC-123, ABC-124, XYZ-789, DEF-456)
- Different availability quantities (including zero)

## Mocking

The tests mock:
- `IInventoryRepository`: To control data returned from the repository
- `IConfiguration`: To control configuration values (failure rate, simulated delay)

This ensures tests are isolated and don't depend on external data sources or actual delays.
