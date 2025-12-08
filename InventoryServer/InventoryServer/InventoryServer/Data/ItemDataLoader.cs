using InventoryServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace InventoryServer.Data
{
    public static class ItemDataLoader
    {
        public static List<InventoryItem> LoadItems(string dataPath = "Data/items.json")
        {
            try
            {
                if (!File.Exists(dataPath))
                {
                    Console.WriteLine($"Warning: Data file not found at {dataPath}");
                    return new List<InventoryItem>();
                }

                var json = File.ReadAllText(dataPath);

                // Deserialize with case-insensitive property matching
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Handle JSON structure with "items" wrapper
                var jsonDoc = JsonDocument.Parse(json);
                var itemsArray = jsonDoc.RootElement.GetProperty("items");
                
                var items = JsonSerializer.Deserialize<List<InventoryItem>>(itemsArray.GetRawText(), options);
                
                return items ?? new List<InventoryItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading items: {ex.Message}");
                return new List<InventoryItem>();
            }
        }
    }
}
