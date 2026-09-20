using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace TransitAI.Services.MenuServices
{
    public class MenuStateService: IDisposable
    {
        private readonly NavigationManager _navigationManager;
        private string _currentMenuTitle = "Tableau de bord réseau";

        public MenuStateService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            // On s'abonne aux changements d'URL
            _navigationManager.LocationChanged += OnLocationChanged;
        
            // Initialisation du titre lors du premier chargement ou d'un rafraîchissement
            UpdateTitleFromUrl(_navigationManager.Uri);
        }

        public string CurrentMenuTitle 
        { 
            get => _currentMenuTitle; 
            set 
            { 
                if (_currentMenuTitle == value) return;
                Console.WriteLine($"[MenuStateService] Changement de titre: '{_currentMenuTitle}' -> '{value}'");
                _currentMenuTitle = value;
                // On notifie les abonnés APRÈS la modification
                OnMenuChanged?.Invoke();
                Console.WriteLine($"[MenuStateService] Nombre d'abonnés: {OnMenuChanged?.GetInvocationList().Length ?? 0}");
            } 
        }

        public event Action? OnMenuChanged;
        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            UpdateTitleFromUrl(e.Location);
        }
        private void UpdateTitleFromUrl(string url)
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath.ToLower();

            // Mapping des URL vers les titres correspondants
            string newTitle = path switch
            {
                "/planification" => "Planification réseau",
                "/realtime-operation" => "Opérations temps réel",
                "/ai-assistant" => "Assistant IA",
                "/agents" => "Utilisateurs",
                "/security" => "Sécurité",
                "/lines" => "Lignes & Arrêts",
                "/fleet" => "Flotte de véhicules",
                "/analytics" => "Fréquentation",
                "/horaires" => "Respect des horaires",
                "/dispatching" => "Dispatching",
                "/alertes" => "Alertes",
                "/parametres" => "Paramètres",
                "/delegations" => "Délégations",
                _ => "Tableau de bord réseau"
            };

            CurrentMenuTitle = newTitle;
        }
        public void SetMenuTitle(string title)
        {
            Console.WriteLine($"[MenuStateService] SetMenuTitle appelé avec: '{title}'");
            CurrentMenuTitle = title;
          }
        public void Dispose()
        {
            // Nettoyage de l'événement pour éviter les fuites de mémoire
            _navigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}