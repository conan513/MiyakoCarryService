using MiyakoCarryService.Client.Events;

namespace MiyakoCarryService.Client.Mgrs
{
    /// <summary>
    /// Formerly the teammate outline highlight system.
    /// Replaced by the Fika-style nameplate (McsFikaHealthBar) in the Fika addon.
    /// </summary>
    public class HighlightMgr : BaseMgr
    {
        public override void Start()
        {
            base.Start();
            EventMgr.Subscribe<GameWorldStartedEvent>(OnGameWorldStarted, this);
            EventMgr.Subscribe<GameWorldEndedEvent>(OnGameWorldEnded, this);
        }

        public override void OnGameWorldStarted(GameWorldStartedEvent @event)
        {
            base.OnGameWorldStarted(@event);
        }

        public override void OnGameWorldEnded(GameWorldEndedEvent @event)
        {
            base.OnGameWorldEnded(@event);
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
        }
    }
}
