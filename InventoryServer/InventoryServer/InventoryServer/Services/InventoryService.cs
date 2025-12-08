using InventoryServer.Models;
using InventoryServer.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace InventoryServer.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _repository;
        private readonly IConfiguration _configuration;

        public InventoryService(IInventoryRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }

        public async Task<SearchResult> SearchInventoryAsync(InventorySearchRequest request)
        {
            // Simulate random errors for testing (configurable)
            var failureRate = _configuration.GetValue<double>("FailureRate", 0.0);
            if (failureRate > 0 && new Random().NextDouble() < failureRate)
            {
                throw new InvalidOperationException("Simulated service failure for testing");
            }

            // Simulate network delay for testing (configurable)
            var simulatedDelay = _configuration.GetValue<int>("SimulatedDelay", 0);
            if (simulatedDelay > 0)
            {
                await Task.Delay(simulatedDelay);
            }

            // Start with all items - like: SELECT * FROM inventory
            var query = (await _repository.GetAllItemsAsync()).AsQueryable();

            // WHERE {field} CONTAINS {criteria}
            if (!string.IsNullOrWhiteSpace(request.Criteria))
            {
                query = ApplySearchCriteria(query, request.By, request.Criteria);
            }

            // AND branch IN (...)
            if (!string.IsNullOrWhiteSpace(request.Branches))
            {
                var branches = request.Branches.Split(',').Select(b => b.Trim().ToUpper()).ToHashSet();
                query = query.Where(i => branches.Contains(i.Branch.ToUpper()));
            }

            // AND availableQty > 0
            if (request.OnlyAvailable)
            {
                query = query.Where(i => i.AvailableQty > 0);
            }

            // COUNT(*)
            var total = query.Count();

            // ORDER BY {field} {direction}
            if (!string.IsNullOrWhiteSpace(request.Sort))
            {
                query = ApplySorting(query, request.Sort);
            }

            // LIMIT {size} OFFSET {page * size}
            var items = query
                .Skip(request.Page * request.Size)
                .Take(request.Size)
                .ToList();

            return new SearchResult
            {
                Total = total,
                Items = items
            };
        }
        private static IQueryable<InventoryItem> ApplySearchCriteria(IQueryable<InventoryItem> query, string searchField, string criteria)
        {
            // Map of searchable string fields - dynamic property selection
            var searchableFields = new Dictionary<string, Expression<Func<InventoryItem, bool>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["partNumber"] = i => i.PartNumber.Contains(criteria, StringComparison.OrdinalIgnoreCase),
                ["supplierSku"] = i => i.SupplierSku.Contains(criteria, StringComparison.OrdinalIgnoreCase),
                ["description"] = i => i.Description.Contains(criteria, StringComparison.OrdinalIgnoreCase)
            };

            var field = searchField ?? "partNumber";
            
            if (searchableFields.TryGetValue(field, out var predicate))
            {
                return query.Where(predicate);
            }

            // Default to partNumber if invalid field
            return query.Where(i => i.PartNumber.Contains(criteria, StringComparison.OrdinalIgnoreCase));
        }

        private static IOrderedQueryable<InventoryItem> ApplySorting(IQueryable<InventoryItem> query, string sortExpression)
        {
            var sortParts = sortExpression.Split(':');
            var field = sortParts[0];
            var isDescending = sortParts.Length > 1 && sortParts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

            // Map sortable fields to property selectors
            return field.ToLower() switch
            {
                "partnumber" => isDescending ? query.OrderByDescending(i => i.PartNumber) : query.OrderBy(i => i.PartNumber),
                "suppliersku" => isDescending ? query.OrderByDescending(i => i.SupplierSku) : query.OrderBy(i => i.SupplierSku),
                "description" => isDescending ? query.OrderByDescending(i => i.Description) : query.OrderBy(i => i.Description),
                "branch" => isDescending ? query.OrderByDescending(i => i.Branch) : query.OrderBy(i => i.Branch),
                "availableqty" => isDescending ? query.OrderByDescending(i => i.AvailableQty) : query.OrderBy(i => i.AvailableQty),
                "uom" => isDescending ? query.OrderByDescending(i => i.Uom) : query.OrderBy(i => i.Uom),
                "leadtimedays" => isDescending ? query.OrderByDescending(i => i.LeadTimeDays) : query.OrderBy(i => i.LeadTimeDays),
                "lastpurchasedate" => isDescending ? query.OrderByDescending(i => i.LastPurchaseDate) : query.OrderBy(i => i.LastPurchaseDate),
                _ => query.OrderBy(i => i.PartNumber) // Default sort
            };
        }

        public async Task<AvailabilityResult> GetPeakAvailabilityAsync(string partNumber)
        {
            // SQL-style: SELECT branch, SUM(availableQty) FROM inventory 
            //            WHERE partNumber = @partNumber GROUP BY branch
            var items = await _repository.FindByPartNumberAsync(partNumber);

            var branches = items.GroupBy(i => i.Branch)
                .Select(g => new BranchAvailability
                {
                    Branch = g.Key,
                    Qty = g.Sum(i => i.AvailableQty)
                })
                .OrderByDescending(b => b.Qty)
                .ToList();

            return new AvailabilityResult
            {
                PartNumber = partNumber,
                TotalAvailable = branches.Sum(b => b.Qty),
                Branches = branches
            };
        }
    }
}
