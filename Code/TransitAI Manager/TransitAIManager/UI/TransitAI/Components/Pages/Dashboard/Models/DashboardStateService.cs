namespace TransitAI.Components.Pages.Dashboard.Models
{
    public class DashboardStateService
    {
        public Incident? CurrentIncident { get; private set; }
        public LiveMetrics Metrics { get; private set; } = new();

        public void InitializeDemoData()
        {
            CurrentIncident = new Incident
            {
                VehicleId = "V1", StationId = "S2", RouteId = "42A",
                ReportedAt = DateTime.Now.AddMinutes(-4),
                PassengersAffected = 23, BackupRequested = true
            };
            Metrics = new LiveMetrics
            {
                OnTimePerformance = 73.3, OnTimeChange = 2.1,
                ActiveVehicles = 11, TotalVehicles = 15
            };
        }
    }
}
