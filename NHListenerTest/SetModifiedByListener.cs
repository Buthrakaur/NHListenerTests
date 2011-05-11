using System.Linq;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Event;

namespace NHListenerTest
{
    public class SetModifiedByListener : IFlushEntityEventListener, ISaveOrUpdateEventListener 
    {
        private readonly ICurrentPrincipalIdProvider currentPrincipalIdProvider;

        public SetModifiedByListener(ICurrentPrincipalIdProvider currentPrincipalIdProvider)
        {
            this.currentPrincipalIdProvider = currentPrincipalIdProvider;
        }

        private void SetModificationDateIfPossible(object entity, ISession session)
        {
            var trackable = entity as IAuditable;
            if (trackable != null)
            {
                trackable.ModifiedBy = session.Load<User>(currentPrincipalIdProvider.GetCurrentPrincipalId());
            }
        }

        public void OnFlushEntity(FlushEntityEvent @event)
        {
            if (@event.EntityEntry.Status == Status.Deleted) return;

            SetModificationDateIfPossible(@event.Entity, @event.Session);
        }

        public void Register(Configuration cfg)
        {
            var listeners = cfg.EventListeners;
            listeners.FlushEntityEventListeners = new IFlushEntityEventListener[] { this }
                .Concat(listeners.FlushEntityEventListeners)
                .ToArray();

            listeners.SaveOrUpdateEventListeners = new ISaveOrUpdateEventListener[] { this }
                .Concat(listeners.SaveOrUpdateEventListeners)
                .ToArray();
        }

        public void OnSaveOrUpdate(SaveOrUpdateEvent @event)
        {
            SetModificationDateIfPossible(@event.Entity, @event.Session);
        }
    }
}