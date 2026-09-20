namespace TransitAI.Components.LogicMetier
{
    public class NotificationDrawerState
    {
        public bool IsNotificationOpen { get; set; } = false;
    
        public event Action? OnChange;

        public void ToggleNotification()
        {
            IsNotificationOpen = !IsNotificationOpen;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke(); 
    }
}