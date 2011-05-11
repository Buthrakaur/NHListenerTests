using System;
using System.Linq;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Event;

namespace NHListenerTest
{
	public class SetModificationTimeFlushEntityEventListener : IFlushEntityEventListener
	{
		public SetModificationTimeFlushEntityEventListener()
		{
			CurrentDateTimeProvider = () => DateTime.Now;
		}

		public Func<DateTime> CurrentDateTimeProvider { get; set; }

		private void SetModificationDateIfPossible(object entity)
		{
			var trackable = entity as IAuditable;
			if (trackable != null)
			{
				trackable.LastModified = CurrentDateTimeProvider();
			}
		}

		public void OnFlushEntity(FlushEntityEvent @event)
		{
			if (@event.EntityEntry.Status == Status.Deleted) return;

			SetModificationDateIfPossible(@event.Entity);
		}

		public void Register(Configuration cfg)
		{
			var listeners = cfg.EventListeners;
			listeners.FlushEntityEventListeners = new IFlushEntityEventListener[] {this}
				.Concat(listeners.FlushEntityEventListeners)
				.ToArray();
		}
	}
}
