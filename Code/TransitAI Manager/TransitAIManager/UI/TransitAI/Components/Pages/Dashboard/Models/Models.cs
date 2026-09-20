namespace TransitAI.Components.Pages.Dashboard.Models
{
    public class TransitRoute
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#000000";
    }

    public class Incident
    {
        public string VehicleId { get; set; } = string.Empty;
        public string StationId { get; set; } = string.Empty;
        public string RouteId { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; }
        public int PassengersAffected { get; set; }
        public bool BackupRequested { get; set; }
        public string TimeAgo => GetTimeAgo(ReportedAt);
        private static string GetTimeAgo(DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} mins ago";
            return $"{(int)diff.TotalHours} hours ago";
        }
    }

    public class LiveMetrics
    {
        public double OnTimePerformance { get; set; }
        public double OnTimeChange { get; set; }
        public int ActiveVehicles { get; set; }
        public int TotalVehicles { get; set; }
    }
}
