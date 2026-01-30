using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;

namespace TmtInventoryApp.Controllers
{
    public class ActivityLogController : Controller
    {
        private readonly InventoryContext _context;

        public ActivityLogController(InventoryContext context)
        {
            _context = context;
        }

        // GET: ActivityLog/DailySummary
        public async Task<IActionResult> DailySummary(DateTime? date)
        {
            // Default to today if no date specified
            var targetDate = date ?? DateTime.Today;
            
            ViewBag.SelectedDate = targetDate;

            // Get all activities for the selected date
            var activities = await _context.ActivityLogs
                .Where(a => a.Timestamp.Date == targetDate.Date)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            // Group activities by type for summary
            var summary = activities.GroupBy(a => a.ActivityType)
                .Select(g => new
                {
                    ActivityType = g.Key,
                    Count = g.Count(),
                    Items = g.ToList()
                })
                .ToList();

            ViewBag.Summary = summary;
            ViewBag.TotalActivities = activities.Count;

            return View(activities);
        }

        // GET: ActivityLog/Index
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string? activityType)
        {
            var query = _context.ActivityLogs.AsQueryable();

            // Default to last 7 days if no dates specified
            var start = startDate ?? DateTime.Today.AddDays(-7);
            var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

            query = query.Where(a => a.Timestamp >= start && a.Timestamp <= end);

            if (!string.IsNullOrEmpty(activityType))
            {
                query = query.Where(a => a.ActivityType == activityType);
            }

            var activities = await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.ActivityType = activityType;

            // Get distinct activity types for filter dropdown
            ViewBag.ActivityTypes = await _context.ActivityLogs
                .Select(a => a.ActivityType)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            return View(activities);
        }
    }
}
