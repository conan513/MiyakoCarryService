

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace MiyakoCarryService.Server.Services
{
    [Injectable(InjectionType.Singleton)]
    public class CompatibilityService(
        IReadOnlyList<SptMod> loadedMods
    )
    {
        public bool HasFikaServer { get; private set; } = false;
        public bool HasAPBS { get; private set; } = false;
        public Type FikaMatchServiceType { get; private set; } = null;
        public Type FikaMatchType { get; private set; } = null;

        private Type _apbsRaidInfoType = null;
        private PropertyInfo _apbsCurrentSessionIdProp = null;
        private PropertyInfo _apbsCurrentRaidLevelProp = null;
        private PropertyInfo _apbsHighestPrestigeLevelProp = null;
        private PropertyInfo _apbsRaidLocationProp = null;
        private PropertyInfo _apbsFreshProfileProp = null;

        public async Task OnPostLoadAsync()
        {
            HasFikaServer = loadedMods.Any(mod => mod.ModMetadata.ModGuid == "Fika");
            if (HasFikaServer)
            {
                CheckFikaServerType();
            }
            HasAPBS = loadedMods.Any(mod => mod.ModMetadata.ModGuid == "com.acidphantasm.progressivebotsystem");
            if (HasAPBS)
            {
                CheckApbsType();
            }
        }

        private void CheckFikaServerType()
        {
            FikaMatchServiceType = Type.GetType("FikaServer.Services.MatchService, FikaServer");
            FikaMatchType = Type.GetType("FikaServer.Models.Fika.FikaMatch, FikaServer");
        }

        private void CheckApbsType()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "acidphantasm-progressivebotsystem");
            _apbsRaidInfoType = asm?.GetType("ProgressiveBotSystem.Models.RaidInformation")
                ?? Type.GetType("ProgressiveBotSystem.Models.RaidInformation, acidphantasm-progressivebotsystem");

            if (_apbsRaidInfoType != null)
            {
                _apbsCurrentSessionIdProp = _apbsRaidInfoType.GetProperty("CurrentSessionId", BindingFlags.Public | BindingFlags.Static);
                _apbsCurrentRaidLevelProp = _apbsRaidInfoType.GetProperty("CurrentRaidLevel", BindingFlags.Public | BindingFlags.Static);
                _apbsHighestPrestigeLevelProp = _apbsRaidInfoType.GetProperty("HighestPrestigeLevel", BindingFlags.Public | BindingFlags.Static);
                _apbsRaidLocationProp = _apbsRaidInfoType.GetProperty("RaidLocation", BindingFlags.Public | BindingFlags.Static);
                _apbsFreshProfileProp = _apbsRaidInfoType.GetProperty("FreshProfile", BindingFlags.Public | BindingFlags.Static);
            }
        }

        public void SetupApbsContext(string sessionId, int playerLevel, int prestigeLevel = 0, string location = "bigmap")
        {
            if (!HasAPBS)
            {
                return;
            }

            if (_apbsRaidInfoType == null)
            {
                CheckApbsType();
            }

            try
            {
                _apbsCurrentSessionIdProp?.SetValue(null, sessionId);
                _apbsCurrentRaidLevelProp?.SetValue(null, (int?)playerLevel);
                _apbsHighestPrestigeLevelProp?.SetValue(null, prestigeLevel);
                _apbsRaidLocationProp?.SetValue(null, string.IsNullOrEmpty(location) ? "bigmap" : location);
                _apbsFreshProfileProp?.SetValue(null, false);
            }
            catch
            {
                // Safe ignore
            }
        }
    }
}