using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace Rent_A_Train
{
    [FileLocation(nameof(Rent_A_Train))]
    [SettingsUIGroupOrder("Interface", "Fleet", "Pricing", "Defaults", "Diagnostics")]
    [SettingsUIShowGroupName("Interface", "Fleet", "Pricing", "Defaults", "Diagnostics")]
    public class Setting : ModSetting
    {
        public enum PanelPosition { Center, UpperLeft }
        [SettingsUISection("General", "Interface")]
        public bool UseUniversalModMenu { get; set; }
        [SettingsUISection("General", "Interface")]
        public PanelPosition InitialWindowPosition { get; set; }
        public Setting(IMod mod) : base(mod) { SetDefaults(); }
        [SettingsUISection("Buses", "Fleet")]
        [SettingsUISlider(min = 1, max = 100, step = 1)]
        public int BusMaximum { get; set; }
        [SettingsUISection("Buses", "Pricing")]
        [SettingsUISlider(min = 100, max = 10000, step = 100)]
        public int BusDailyPrice { get; set; }
        [SettingsUISection("Trains", "Fleet")]
        [SettingsUISlider(min = 1, max = 100, step = 1)]
        public int TrainMaximum { get; set; }
        [SettingsUISection("Trains", "Pricing")]
        [SettingsUISlider(min = 100, max = 10000, step = 100)]
        public int TrainDailyPrice { get; set; }
        [SettingsUISection("General", "Defaults")]
        [SettingsUIButton]
        public bool ResetDefaults { set { if (value) { SetDefaults(); ApplyAndSave(); } } }
        [SettingsUISection("General", "Diagnostics")]
        public string Status => RentalDepotSystem.Status;
        [SettingsUISection("General", "Diagnostics")]
        [SettingsUIButton]
        public bool WriteDiagnostics { set { if (value) RentalDepotSystem.DiagnosticsRequested = true; } }
        [SettingsUISection("General", "Diagnostics")]
        [SettingsUIButton]
        public bool RemoveRentalData { set { if (value) RentalDepotSystem.CleanupRequested = true; } }
        public override void SetDefaults()
        {
            UseUniversalModMenu = false; InitialWindowPosition = PanelPosition.Center;
            BusMaximum = 20; TrainMaximum = 10;
            BusDailyPrice = 1000; TrainDailyPrice = 5000;
        }
        internal int Limit(bool bus) => System.Math.Max(1, System.Math.Min(100, bus ? BusMaximum : TrainMaximum));
        internal int Price(bool bus) => System.Math.Max(100, System.Math.Min(10000, bus ? BusDailyPrice : TrainDailyPrice));
    }
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting setting;
        public LocaleEN(Setting setting) { this.setting = setting; }
        public IEnumerable<KeyValuePair<string,string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string,int> indexCounts)
        {
            var result = new Dictionary<string,string>
            {
                {setting.GetSettingsLocaleID(), "Rent a Train"},
                {setting.GetOptionTabLocaleID("Buses"), "Buses"},
                {setting.GetOptionTabLocaleID("Trains"), "Trains"},
                {setting.GetOptionTabLocaleID("General"), "General"},
                {setting.GetOptionGroupLocaleID("Interface"), "Interface"},
                {setting.GetOptionLabelLocaleID(nameof(Setting.UseUniversalModMenu)), "Use Universal Mod Menu"},
                {setting.GetOptionDescLocaleID(nameof(Setting.UseUniversalModMenu)), "Show Rent a Train in the Universal Mod Menu instead of beside the infoview button at the top left."},
                {setting.GetOptionLabelLocaleID(nameof(Setting.InitialWindowPosition)), "Initial window position"},
                {setting.GetOptionDescLocaleID(nameof(Setting.InitialWindowPosition)), "Choose where the rental window opens. Drag its title bar to move it freely."},
                {setting.GetEnumValueLocaleID(Setting.PanelPosition.Center), "Center"},
                {setting.GetEnumValueLocaleID(Setting.PanelPosition.UpperLeft), "Upper left"},
                {setting.GetOptionGroupLocaleID("Fleet"), "Rental limit"},
                {setting.GetOptionGroupLocaleID("Pricing"), "Monthly rental price"},
                {setting.GetOptionGroupLocaleID("Defaults"), "Default settings"},
                {setting.GetOptionGroupLocaleID("Diagnostics"), "Diagnostics"},
                {setting.GetOptionLabelLocaleID(nameof(Setting.BusMaximum)), "Maximum rented buses"},
                {setting.GetOptionLabelLocaleID(nameof(Setting.TrainMaximum)), "Maximum rented train sets"},
                {setting.GetOptionLabelLocaleID(nameof(Setting.BusDailyPrice)), "Price per bus per month"},
                {setting.GetOptionLabelLocaleID(nameof(Setting.TrainDailyPrice)), "Price per train set per month"},
                {setting.GetOptionLabelLocaleID(nameof(Setting.ResetDefaults)), "Reset to defaults"},
                {setting.GetOptionDescLocaleID(nameof(Setting.ResetDefaults)), "Restore limits of 20 buses and 10 train sets, with monthly prices of 1,000 and 5,000 respectively. Existing rental quantities are kept within the restored limits."},
                {setting.GetOptionLabelLocaleID(nameof(Setting.Status)), "Rental status"},
                {setting.GetOptionDescLocaleID(nameof(Setting.Status)), "Use the cyan train-and-dollar button at the top left of the game to manage your rented fleet."},
                {setting.GetOptionLabelLocaleID(nameof(Setting.WriteDiagnostics)), "Write diagnostic report"},
                {setting.GetOptionDescLocaleID(nameof(Setting.WriteDiagnostics)), "Resume the simulation to write connection and fleet details to Logs/RentATrain.log."},
                {setting.GetOptionLabelLocaleID(nameof(Setting.RemoveRentalData)), "Remove inactive rental data"},
                {setting.GetOptionDescLocaleID(nameof(Setting.RemoveRentalData)), "Set both rental quantities to zero in the rental panel, resume and wait for every rented vehicle to return. Then remove inactive rental data, resume, save and exit before disabling the mod. Infrastructure unlocks remain."},
                {"EconomyPanel.BUDGET_ITEM[Rented vehicles]", "Rented vehicles"},
                {"EconomyPanel.BUDGET_ITEM_DESCRIPTION[Rented vehicles]", "Rental of buses and train sets from outside connections. Includes unused contracted vehicles. Prices are set in Rent a Train options."},
                {"EconomyPanel.BUDGET_SUB_ITEM[Rented buses]", "Rented buses"},
                {"EconomyPanel.BUDGET_SUB_ITEM[Rented trains]", "Rented trains"}
            };
            foreach (var name in new[]{nameof(Setting.BusMaximum),nameof(Setting.TrainMaximum)})
                result[setting.GetOptionDescLocaleID(name)] = "Maximum available in the in-game rental panel (1–100). Lowering this limit reduces an existing rental to match; excess vehicles return through normal game logic.";
            foreach (var name in new[]{nameof(Setting.BusDailyPrice),nameof(Setting.TrainDailyPrice)})
                result[setting.GetOptionDescLocaleID(name)] = "Cost per vehicle per month (100–10,000). Applies to all rented capacity, including unused slots. Changes take effect when the simulation resumes.";
            return result;
        }
        public void Unload() { }
    }
}
