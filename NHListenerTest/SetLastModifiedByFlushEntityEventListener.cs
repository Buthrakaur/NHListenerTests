using System;
using System.Linq;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Event;

namespace NHListenerTest
{
    public class SetLastModifiedByFlushEntityEventListener : IFlushEntityEventListener, ISaveOrUpdateEventListener 
    {
        public Func<long> CurrentUserIdProvider { get; set; }

        private void SetModificationDateIfPossible(object entity, ISession session)
        {
            var trackable = entity as ITrackModificationDate;
            if (trackable != null)
            {
                trackable.LastModifiedBy = session.Load<User>(CurrentUserIdProvider());
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