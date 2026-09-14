using System.Reflection;
using EFT;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using MiyakoCarryService.Client.Utils;
using SPT.Reflection.Patching;

namespace MiyakoCarryService.Client.Patches.SAIN
{
    /// <summary>
    /// 当护航处于Mcs层接管期间，强制让SAIN的IsBotInCombat返回false，
    /// 从而防止SAIN的BotMoverManualFixedUpdatePatch阻断EFT原生的BotMover.ManualFixedUpdate
    /// </summary>
    public sealed class SAINIsBotInCombatPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(SAINUtils.SAINEnableClassType, "IsBotInCombat", new[] { typeof(IPlayer) });

        private static McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static bool Prefix(IPlayer player, ref bool __result)
        {
            if (player == null)
            {
                return true;
            }

            if (McsMgr.IsMcsBotPlayer(player.ProfileId))
            {
                var botOwner = player.AIData?.BotOwner;
                if (botOwner != null)
                {
                    var mcsBotPlayerData = botOwner.GetMcsBotPlayerData();
                    if (mcsBotPlayerData != null && mcsBotPlayerData.IsMcsLayerActive)
                    {
                        __result = false;
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
