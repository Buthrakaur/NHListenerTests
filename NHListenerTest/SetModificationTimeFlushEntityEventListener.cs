using System;
using System.Linq;
using System.Text;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Event.Default;
using NHibernate.Persister.Entity;

namespace NHListenerTest
{
	public interface IModificationTimeListener
	{
		Func<DateTime> CurrentDateTimeProvider { get; set; }
		void Register(Configuration cfg);
	}

	public class SetModificationTimeFlushEntityEventListener : IFlushEntityEventListener, IModificationTimeListener
	{
		public SetModificationTimeFlushEntityEventListener()
		{
			CurrentDateTimeProvider = () => DateTime.Now;
		}

		public Func<DateTime> CurrentDateTimeProvider { get; set; }

		private void SetModificationDateIfPossible(object entity)
		{
			var trackable = entity as ITrackModificationDate;
			if (trackable != null)
			{
				trackable.LastModified = CurrentDateTimeProvider();
			}
		}

		public void OnFlushEntity(FlushEntityEvent @event)
		{
			if (@event.EntityEntry.Status != Status.Deleted &&
				(!@event.EntityEntry.ExistsInDatabase || @event.Session.IsDirtyEntity(@event.Entity)))
			{
				SetModificationDateIfPossible(@event.Entity);
			}
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
