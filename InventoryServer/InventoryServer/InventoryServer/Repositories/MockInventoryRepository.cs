using InventoryServer.Data;
using InventoryServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryServer.Repositories
{
    public class MockInventoryRepository : IInventoryRepository
    {
        // In-memory data source - acts like a database table for LINQ queries
        private readonly List<InventoryItem> _items;
        
        // Index for optimized lookups - simulates database index
        private readonly Dictionary<string, List<InventoryItem>> _partNumberIndex;

        public MockInventoryRepository()
        {
            // Load data from JSON file using DataLoader
            _items = ItemDataLoader.LoadItems();
            
            // Build index for fast lookups (like SQL: CREATE INDEX idx_partnumber ON inventory(partNumber))
            _partNumberIndex = _items
                .GroupBy(i => i.PartNumber.ToUpper())
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // SQL equivalent: SELECT * FROM inventory
        public Task<List<InventoryItem>> GetAllItemsAsync()
        {
            return Task.FromResult(_items);
        }

        // SQL equivalent: SELECT * FROM inventory WHERE partNumber = @partNumber
        // Uses index for O(1) lookup instead of O(n) table scan
        public Task<List<InventoryItem>> FindByPartNumberAsync(string partNumber)
        {
            if (string.IsNullOrWhiteSpace(partNumber))
                return Task.FromResult(new List<InventoryItem>());

            // Index lookup (fast!)
            var key = partNumber.ToUpper();
            var result = _partNumberIndex.TryGetValue(key, out var items) 
                ? items 
                : new List<InventoryItem>();
            
            return Task.FromResult(result);
        }

        // Additional SQL-style helper methods for LINQ demonstrations

        /// <summary>
        /// SQL: SELECT * FROM inventory WHERE branch IN (@branches)
        /// </summary>
        public IEnumerable<InventoryItem> GetItemsByBranches(params string[] branches)
        {
            var branchSet = branches.Select(b => b.ToUpper()).ToHashSet();
            return _items.Where(i => branchSet.Contains(i.Branch.ToUpper()));
        }

        /// <summary>
        /// SQL: SELECT * FROM inventory WHERE availableQty > 0
        /// </summary>
        public IEnumerable<InventoryItem> GetAvailableItems()
        {
            return _items.Where(i => i.AvailableQty > 0);
        }
    }
}
