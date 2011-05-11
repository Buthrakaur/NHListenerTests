using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Cfg;

namespace NHListenerTest
{
	public class ModificationTimeLoggingInterceptor: EmptyInterceptor, IModificationTimeListener
	{
		public Func<DateTime> CurrentDateTimeProvider { get; set; }
		public void Register(Configuration cfg)
		{
			cfg.SetInterceptor(this);
		}

		private void SetModificationDateIfPossible(object entity, ref object[] currentState, string[] propertyNames)
		{
			var trackable = entity as ITrackModificationDate;
			if (trackable == null) return;

			trackable.LastModified = CurrentDateTimeProvider();
			currentState[Array.IndexOf(propertyNames, "LastModified")] = trackable.LastModified;
		}

		public override bool OnFlushDirty(object entity, object id, object[] currentState, object[] previousState, string[] propertyNames, NHibernate.Type.IType[] types)
		{
			SetModificationDateIfPossible(entity, ref currentState, propertyNames);
			return base.OnFlushDirty(entity, id, currentState, previousState, propertyNames, types);
		}

		public override bool OnSave(object entity, object id, object[] state, string[] propertyNames, NHibernate.Type.IType[] types)
		{
			SetModificationDateIfPossible(entity, ref state, propertyNames);
			return base.OnSave(entity, id, state, propertyNames, types);
		}
	}
}
