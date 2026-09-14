using System;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using MiyakoCarryService.Client.Extensions;

namespace MiyakoCarryService.Client.Bots.Brain.Logics
{
    public class GoToPointLogic : McsBotBaseLogic
    {
        private GoToSomePoint _baseLogic;

        public GoToPointLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            if (BotOwner.Mover != null && BotOwner.Mover.Pause)
            {
                BotOwner.Mover.Pause = false;
            }

            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                BotOwner.GoToSomePointData.UpdateToGo(true, 1f, 1f);
                return;
            }

            var leadPlayer = mcsBotPlayerData.LeadPlayer;
            if (leadPlayer == null)
            {
                BotOwner.GoToSomePointData.UpdateToGo(true, 1f, 1f);
                return;
            }

            var botToLeaderSqrDistance = BotOwner.Position.McsSqrDistance(leadPlayer.Position);
            BotOwner.Steering.LookToMovingDirection();
            var leaderMovementContext = leadPlayer.MovementContext;
            var botWithin50 = botToLeaderSqrDistance < 50f * 50f;
            var isCloseToLead = botToLeaderSqrDistance <= 4f * 4f;

            if (!BotOwner.Memory.HaveEnemy && botWithin50 && leaderMovementContext != null)
            {
                _baseLogic.DoorOpen();
                if (leaderMovementContext.IsInPronePose)
                {
                    BotOwner.BotLight?.TurnOff(false, true);
                    BotOwner.GoToSomePointData.UpdateToGo(false, isCloseToLead ? 0f : 0.5f, 0f);
                }
                else if (leaderMovementContext.PoseLevel < 1f)
                {
                    BotOwner.BotLight?.TurnOff(false, true);
                    var speed = isCloseToLead ? Math.Max(0.4f, leaderMovementContext.CharacterMovementSpeed) : 1f;
                    BotOwner.GoToSomePointData.UpdateToGo(false, speed, leaderMovementContext.PoseLevel);
                }
                else
                {
                    var sprint = botToLeaderSqrDistance > 15f * 15f;
                    var speed = isCloseToLead ? (leaderMovementContext.CharacterMovementSpeed > 0.1f ? leaderMovementContext.CharacterMovementSpeed : 0.8f) : 1f;
                    BotOwner.GoToSomePointData.UpdateToGo(sprint, speed, 1f);
                }
            }
            else
            {
                BotOwner.GoToSomePointData.UpdateToGo(true, 1f, 1f);
            }
        }
    }
}