using System;
using System.Collections;
using System.Linq;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Intercept;
using NHibernate.Persister.Entity;

namespace NHListenerTest
{
	public class SetModificationTimeFlushEntityEventListener : IFlushEntityEventListener, ISaveOrUpdateEventListener, IMergeEventListener
	{
		public SetModificationTimeFlushEntityEventListener()
		{
			CurrentDateTimeProvider = () => DateTime.Now;
		}

		public Func<DateTime> CurrentDateTimeProvider { get; set; }

		public void OnFlushEntity(FlushEntityEvent @event)
		{
			var entity = @event.Entity;
			var entityEntry = @event.EntityEntry;

			if (entityEntry.Status == Status.Deleted)
			{
				return;
			}
			var trackable = entity as ITrackModificationDate;
			if (trackable == null)
			{
				return;
			}
			if (HasDirtyProperties(@event))
			{
				trackable.LastModified = CurrentDateTimeProvider();
			}
		}

		private bool HasDirtyProperties(FlushEntityEvent @event)
		{
			ISessionImplementor session = @event.Session;
			EntityEntry entry = @event.EntityEntry;
			var entity = @event.Entity;
			if (!entry.RequiresDirtyCheck(entity) || !entry.ExistsInDatabase || entry.LoadedState == null)
			{
				return false;
			}
			IEntityPersister persister = entry.Persister;

			object[] currentState = persister.GetPropertyValues(entity, session.EntityMode); ;
			object[] loadedState = entry.LoadedState;

			return persister.EntityMetamodel.Properties
				.Where((property, i) => !LazyPropertyInitializer.UnfetchedProperty.Equals(currentState[i]) && property.Type.IsDirty(loadedState[i], currentState[i], session))
				.Any();
		}

		public void OnSaveOrUpdate(SaveOrUpdateEvent @event)
		{
			ExplicitUpdateCall(@event.Entity as ITrackModificationDate);
		}

		public void OnMerge(MergeEvent @event)
		{
			ExplicitUpdateCall(@event.Entity as ITrackModificationDate);
		}

		public void OnMerge(MergeEvent @event, IDictionary copiedAlready)
		{
			ExplicitUpdateCall(@event.Entity as ITrackModificationDate);
		}

		private void ExplicitUpdateCall(ITrackModificationDate trackable)
		{
			if (trackable == null)
			{
				return;
			}
			trackable.LastModified = CurrentDateTimeProvider();
		}

		public void Register(Configuration cfg)
		{
			var listeners = cfg.EventListeners;
			listeners.FlushEntityEventListeners = new [] {this}
				.Concat(listeners.FlushEntityEventListeners)
				.ToArray();
			listeners.SaveEventListeners = new[] {this}
				.Concat(listeners.SaveEventListeners)
				.ToArray();
			listeners.SaveOrUpdateEventListeners = new[] {this}
				.Concat(listeners.SaveOrUpdateEventListeners)
				.ToArray();
			listeners.UpdateEventListeners = new[] {this}
				.Concat(listeners.UpdateEventListeners)
				.ToArray();
			listeners.MergeEventListeners = new[] {this}
				.Concat(listeners.MergeEventListeners)
				.ToArray();
		}
	}
}
