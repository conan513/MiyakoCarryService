
using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using McsProfileController = MiyakoCarryService.Server.Controllers.ProfileController;
using McsRaidController = MiyakoCarryService.Server.Controllers.RaidController;

namespace MiyakoCarryService.Server.Patches.Group
{
    /// <summary>
    /// 战局结束时如果类型不是转移，则清空该玩家的小队成员
    /// Raid vége után XP-t ad a companion botoknak és frissíti szintjüket.
    /// </summary>
    [Injectable]
    public sealed class EndLocalRaidPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchController), nameof(MatchController.EndLocalRaidAsync));

        public EndLocalRaidPatch(McsRaidController raidController, McsProfileController profileController)
        {
            _raidController = raidController;
            _profileController = profileController;
        }

        private static McsRaidController _raidController;
        private static McsProfileController _profileController;

        [PatchPrefix]
        public static void Prefix(MongoId sessionId, EndLocalRaidRequestData request)
        {
            var isTransfer = request.Results.IsMapToMapTransfer();
            if (isTransfer)
            {
                return;
            }
            _raidController.ClearGroupMember(sessionId);

            // Raid végén random 1000-10000 XP a companion botoknak
            _profileController.AwardRaidXpToCompanionBots(sessionId);

            // Raid végén 2 random companion bot kicserélése friss, újgenerált botokra
            _profileController.RotateOneCompanionBot(sessionId, 2);
        }
    }
}