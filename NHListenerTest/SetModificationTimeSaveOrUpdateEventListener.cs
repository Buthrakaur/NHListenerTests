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

	public class SetModificationTimeSaveOrUpdateEventListener : DefaultSaveOrUpdateEventListener, IModificationTimeListener
	{
		public SetModificationTimeSaveOrUpdateEventListener()
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

		protected override object PerformSave(object entity, object id, IEntityPersister persister, bool useIdentityColumn, object anything, IEventSource source, bool requiresImmediateIdAccess)
		{
			SetModificationDateIfPossible(entity);
			return base.PerformSave(entity, id, persister, useIdentityColumn, anything, source, requiresImmediateIdAccess);
		}

		protected override void PerformUpdate(SaveOrUpdateEvent @event, object entity, IEntityPersister persister)
		{
			if (@event.Session.IsDirtyEntity(@event.Entity))
			{
				SetModificationDateIfPossible(entity);
			}
			base.PerformUpdate(@event, entity, persister);
		}

		protected override object PerformSaveOrUpdate(SaveOrUpdateEvent @event)
		{
			if (@event.Session.IsNewEntity(@event.Entity) || @event.Session.IsDirtyEntity(@event.Entity))
			{
				SetModificationDateIfPossible(@event.Entity);
			}
			return base.PerformSaveOrUpdate(@event);
		}

		public void Register(Configuration cfg)
		{
			var listeners = cfg.EventListeners;
			listeners.SaveEventListeners =
				listeners.SaveEventListeners
					.NullToEmpty()
					.Append(this)
					.ToArray();
			listeners.UpdateEventListeners =
				listeners.UpdateEventListeners
					.NullToEmpty()
					.Append(this)
					.ToArray();
			listeners.SaveOrUpdateEventListeners =
				listeners.SaveOrUpdateEventListeners
					.NullToEmpty()
					.Append(this)
					.ToArray();
		}
	}

	public class SetModificationTimeFlushEntityEventListener : DefaultFlushEntityEventListener, IModificationTimeListener
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

		public override void OnFlushEntity(FlushEntityEvent @event)
		{
			if (@event.EntityEntry.Status != Status.Deleted &&
				(!@event.EntityEntry.ExistsInDatabase || @event.Session.IsDirtyEntity(@event.Entity)))
			{
				SetModificationDateIfPossible(@event.Entity);
			}

			base.OnFlushEntity(@event);
		}

		public void Register(Configuration cfg)
		{
			var listeners = cfg.EventListeners;
			listeners.FlushEntityEventListeners = listeners.FlushEntityEventListeners
				.NullToEmpty()
				.Append(this)
				.ToArray();
		}
	}
}
