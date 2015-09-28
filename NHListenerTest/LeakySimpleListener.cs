using NHibernate.Cfg;
using NHibernate.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHListenerTest
{
    // WARNING: http://nhibernate.info/doc/howto/various/changing-values-in-nhibernate-events.html
    // MIGHT FAIL IN SOME SITUATIONS
    public class LeakySimpleListener : IPreInsertEventListener, IPreUpdateEventListener
    {
        #region const

        private const string CHANGED_ON = "LastModified";

        #endregion

        public LeakySimpleListener()
        {
            CurrentDateTimeProvider = () => DateTime.Now;
        }

        public Func<DateTime> CurrentDateTimeProvider { get; set; }


        public bool OnPreInsert(PreInsertEvent args)
        {
            ITrackModificationDate entity = args.Entity as ITrackModificationDate;

            if (entity != null)
            {
                ITrackModificationDate auditEntity = entity as ITrackModificationDate;
                DateTime now = DateTime.Now;

                int idxChangedOn = GetIndex(args.Persister.PropertyNames, CHANGED_ON);

                auditEntity.LastModified = CurrentDateTimeProvider();
                args.State[idxChangedOn] = CurrentDateTimeProvider();
            }

            return false;
        }

        public bool OnPreUpdate(PreUpdateEvent args)
        {
            ITrackModificationDate entity = args.Entity as ITrackModificationDate;

            if (entity != null)
            {

                var dirtyField = args.Persister.FindDirty(args.State, args.OldState, args.Entity, args.Session);

                ITrackModificationDate auditEntity = entity as ITrackModificationDate;
                DateTime now = DateTime.Now;
             
                int idxChangedOn = GetIndex(args.Persister.PropertyNames, CHANGED_ON);
               
                auditEntity.LastModified = CurrentDateTimeProvider();
                args.State[idxChangedOn] = CurrentDateTimeProvider();
            }

            return false;
        }

        private int GetIndex(string[] propertyNames, string property)
        {
            for (var i = 0; i < propertyNames.Length; i++)
            {
                if (propertyNames[i].Equals(property))
                {
                    return i;
                }
            }
            return -1;
        }

        public void Register(Configuration cfg)
        {
            var listeners = cfg.EventListeners;
            listeners.PreInsertEventListeners = new[] { this }
                .Concat(listeners.PreInsertEventListeners)
                .ToArray();
            listeners.PreUpdateEventListeners = new[] { this }
                .Concat(listeners.PreUpdateEventListeners)
                .ToArray();
        }
    }
}
