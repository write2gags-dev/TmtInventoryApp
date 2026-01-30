using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;
using TmtInventoryApp.Models.ViewModels;

namespace TmtInventoryApp.Services
{
    public class StockHistoryService
    {
        private readonly InventoryContext _context;

        public StockHistoryService(InventoryContext context)
        {
            _context = context;
        }

        public async Task<List<StockHistoryViewModel>> GetOfflineAdditionsAsync(DateTime cutoffDate)
        {
            // Use Set<ActivityLog>() to be safe if property missing
            var logs = await _context.Set<ActivityLog>()
                .Where(l => l.ActivityType == "Stock" 
                           && l.Action == "Created" 
                           && l.Timestamp >= cutoffDate)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            var history = new List<StockHistoryViewModel>();

            foreach (var log in logs)
            {
                if (string.IsNullOrEmpty(log.AdditionalDetails)) continue;

                var record = new StockHistoryViewModel
                {
                    Date = log.Timestamp,
                    UserName = log.UserName ?? "Unknown"
                };

                // Parsing Logic
                try 
                {
                    string details = log.AdditionalDetails;
                    int itemsIndex = details.IndexOf("Items: ");
                    if (itemsIndex != -1)
                    {
                        var invPart = details.Substring(0, itemsIndex).Replace("Invoice: ", "").Trim().TrimEnd(',');
                        record.InvoiceNumber = invPart;

                        var itemsPart = details.Substring(itemsIndex + 7); 
                        var rawItems = itemsPart.Split(new[] { "tons, " }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var rawItem in rawItems)
                        {
                            var itemStr = rawItem.Trim();
                            if (!itemStr.EndsWith("tons")) itemStr += " tons";

                            var parts = itemStr.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var variantName = parts[0].Trim();
                                var qtyParts = parts[1].Split(new[] { " bundles, " }, StringSplitOptions.RemoveEmptyEntries);
                                if (qtyParts.Length >= 2)
                                {
                                    int bundles = 0;
                                    decimal weight = 0;
                                    int.TryParse(qtyParts[0].Trim(), out bundles);
                                    var weightStr = qtyParts[1].Replace(" tons", "").Trim();
                                    decimal.TryParse(weightStr, out weight);

                                    if (bundles > 0 || weight > 0)
                                    {
                                        record.Items.Add(new StockHistoryItem
                                        {
                                            VariantName = variantName,
                                            Bundles = bundles,
                                            WeightTons = weight
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                catch { continue; } // Parser fail

                if (record.Items.Any())
                {
                    history.Add(record);
                }
            }
            return history;
        }
    }
}
