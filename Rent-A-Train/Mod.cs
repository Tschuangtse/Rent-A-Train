using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;

namespace Rent_A_Train
{
    public class Mod : IMod
    {
        public static readonly ILog log = LogManager.GetLogger("RentATrain").SetShowsErrorsInUI(false);
        internal static Setting Settings;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Settings = new Setting(this);
            AssetDatabase.global.LoadSettings(nameof(Rent_A_Train), Settings, new Setting(this));
            Settings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));
            updateSystem.UpdateAt<RentalPrefabSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<RentalUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateBefore<RentalDepotSystem, TransportDepotAISystem>(SystemUpdatePhase.GameSimulation);
            log.Info("Rent a Train prototype 0.2.5 loaded. No rental is started automatically.");
        }

        public void OnDispose()
        {
            Settings?.UnregisterInOptionsUI();
            Settings = null;
            RentalDepotSystem.Requests.Clear();
            log.Info("Rent a Train disposed.");
        }
    }
}
