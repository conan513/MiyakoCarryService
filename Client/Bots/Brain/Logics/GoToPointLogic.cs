
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
            var mcsBotPlayerData = BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                BotOwner.Sprint(true, false);
                _baseLogic.UpdateNodeByMain(data);
                return;
            }

            var leadPlayer = mcsBotPlayerData.LeadPlayer;
            if (leadPlayer == null)
            {
                BotOwner.Sprint(true, false);
                _baseLogic.UpdateNodeByMain(data);
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
                    BotOwner.SetPose(0f);
                    BotOwner.SetTargetMoveSpeed(isCloseToLead ? 0f : 0.5f);
                }
                else if (leaderMovementContext.PoseLevel < 1f)
                {
                    BotOwner.BotLight?.TurnOff(false, true);
                    BotOwner.SetPose(leaderMovementContext.PoseLevel);
                    BotOwner.SetTargetMoveSpeed(isCloseToLead ? leaderMovementContext.CharacterMovementSpeed : 1f);
                }
                else
                {
                    BotOwner.SetPose(1f);
                    if (botToLeaderSqrDistance > 15f * 15f)
                    {
                        BotOwner.Sprint(true, false);
                    }
                    else
                    {
                        BotOwner.Sprint(false, false);
                        BotOwner.SetTargetMoveSpeed(isCloseToLead ? leaderMovementContext.CharacterMovementSpeed : 1f);
                    }
                }
            }
            else
            {
                BotOwner.Sprint(true, false);
            }

            _baseLogic.UpdateNodeByMain(data);
        }
    }
}