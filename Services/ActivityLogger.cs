using TmtInventoryApp.Data;
using TmtInventoryApp.Models;

namespace TmtInventoryApp.Services
{
    public class ActivityLogger
    {
        private readonly InventoryContext _context;

        public ActivityLogger(InventoryContext context)
        {
            _context = context;
        }

        public async Task LogActivityAsync(
            string activityType, 
            string action, 
            string description, 
            string? userName = null, 
            string? entityType = null, 
            int? entityId = null, 
            string? additionalDetails = null)
        {
            var log = new ActivityLog
            {
                Timestamp = DateTime.Now,
                ActivityType = activityType,
                Action = action,
                Description = description,
                UserName = userName,
                EntityType = entityType,
                EntityId = entityId,
                AdditionalDetails = additionalDetails
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public void LogActivity(
            string activityType, 
            string action, 
            string description, 
            string? userName = null, 
            string? entityType = null, 
            int? entityId = null, 
            string? additionalDetails = null)
        {
            var log = new ActivityLog
            {
                Timestamp = DateTime.Now,
                ActivityType = activityType,
                Action = action,
                Description = description,
                UserName = userName,
                EntityType = entityType,
                EntityId = entityId,
                AdditionalDetails = additionalDetails
            };

            _context.ActivityLogs.Add(log);
            _context.SaveChanges();
        }
    }
}
