using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;

namespace NHListenerTest
{
	public static class Extensions
	{
		public static void SetPropertyValue(this IEntityPersister persister, object[] state, string propertyName, object value)
		{
			var index = Array.IndexOf(persister.PropertyNames, propertyName);
			if (index < 0) return;
			state[index] = value;
		}

		public static bool IsDirtyEntity(this ISession session, object entity)
		{
			var className = NHibernateProxyHelper.GuessClass(entity).FullName;
			var sessionImpl = session.GetSessionImplementation();
			var persister = sessionImpl.Factory.GetEntityPersister(className);

			var oldEntry = sessionImpl.PersistenceContext.GetEntry(sessionImpl.PersistenceContext.Unproxy(entity));

			if (oldEntry == null) return false;
			var oldState = oldEntry.LoadedState;
			var currentState = persister.GetPropertyValues(entity, sessionImpl.EntityMode);
			var dirtyProps = persister.FindDirty(currentState, oldState, entity, sessionImpl);
			return dirtyProps != null && dirtyProps.Any();
		}

		public static bool IsNewEntity(this ISession session, object entity)
		{
			var sessionImpl = session.GetSessionImplementation();
			var oldEntry = sessionImpl.PersistenceContext.GetEntry(sessionImpl.PersistenceContext.Unproxy(entity));
			return oldEntry == null || !oldEntry.ExistsInDatabase;
		}

		public static IEnumerable<TItem> NullToEmpty<TItem>(this IEnumerable<TItem> enumerable)
		{
			return enumerable ?? new List<TItem>();
		}

		public static IEnumerable<TItem> Append<TItem>(this IEnumerable<TItem> enumerable, TItem item)
		{
			return enumerable.Concat(new[] { item });
		}
	}
}