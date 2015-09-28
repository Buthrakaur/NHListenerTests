using System;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using Xunit;
using log4net;

namespace NHListenerTest
{

    /// <summary>
    /// Uncomment/Comment the listener to see it working (all green with SetModificationTimeFlushEntityEventListener)
    /// </summary>
	public class SetModificationDateIntegrationTests : IDisposable
	{
		private readonly ISessionFactory sessionFactory;
		private readonly ISession session;
		private LeakySimpleListener listener;
        //private SetModificationTimeFlushEntityEventListener listener;
        private readonly DateTime defaultDate = new DateTime(2000, 1, 1);

        private static readonly ILog log = LogManager.GetLogger(typeof(SetModificationDateIntegrationTests));

        public SetModificationDateIntegrationTests()
		{
            log4net.Config.XmlConfigurator.Configure();

            // working Listener
            //listener = new SetModificationTimeFlushEntityEventListener()
            //{
            //    CurrentDateTimeProvider = () => defaultDate
            //};

            //Tests with inheritance - structure and join-tables will fail
            listener = new LeakySimpleListener()
            {
                CurrentDateTimeProvider = () => defaultDate
            };

            Configuration config = null;
			sessionFactory = Fluently.Configure()
				.Database(SQLiteConfiguration.Standard.InMemory)
				.Mappings(m =>
				{
					m.FluentMappings.Add<ThingMap>();
					m.FluentMappings.Add<InheritedThingMap>();
					m.FluentMappings.Add<RelatedThingMap>();
                    m.FluentMappings.Add<SomeThingJoinedMap>();
                })
				.ExposeConfiguration(cfg =>
				{
					listener.Register(cfg);
					config = cfg;
					cfg.SetProperty("show_sql", "true");
                    cfg.SetProperty("format_sql", "true");
				})
				.BuildSessionFactory();

			session = sessionFactory.OpenSession();

			using (var tx = session.BeginTransaction())
			{
				new SchemaExport(config).Execute(false, true, false, session.Connection, null);
				tx.Commit();
			}

			session.BeginTransaction();
		}

		public void Dispose()
		{
			session.Transaction.Rollback();
			session.Dispose();
			sessionFactory.Dispose();
		}

		[Fact]
		public void LastModified_Should_BeSetOnInsert()
		{
			var t = new Thing { Id = 1 };
			session.Save(t);
			session.Flush();

			Assert.Equal(defaultDate, t.LastModified);
		}

		[Fact]
		public void LastModified_Should_BeSetOnUpdate()
		{
			var t = new Thing { Id = 1 };
			session.Save(t);

			session.Flush();
			session.Clear();

			listener.CurrentDateTimeProvider = () => new DateTime(2001, 1, 1);
			t = session.Get<Thing>(1L);
			t.LastModified = DateTime.Now.AddYears(-10);
			session.Update(t);

			session.Flush();

			Assert.Equal(new DateTime(2001, 1, 1), t.LastModified);
		}

		[Fact]
		public void LastModified_Should_BeSetOnImplicitUpdate()
		{
			var t = new Thing { Id = 1 };
			session.Save(t);

			session.Flush();
			session.Clear();

			listener.CurrentDateTimeProvider = () => new DateTime(2001, 1, 1);
			t = session.Get<Thing>(1L);
			t.LastModified = DateTime.Now.AddYears(-10);

			session.Flush();

			Assert.Equal(new DateTime(2001, 1, 1), t.LastModified);
		}

		[Fact]
		public void InheritedThing_LastModified_Should_BeSetOnInsert()
		{
			var t = new InheritedThing { Id = 1, SomeText = "aa" };
			session.Save(t);
			session.Flush();
			Assert.Equal(defaultDate, t.LastModified);
		}

		[Fact]
		public void InheritedThing_LastModified_Should_BeSetOnUpdate()
		{
			var t = new InheritedThing { Id = 1, SomeText = "aa" };
			session.Save(t);

			session.Flush();
			session.Clear();

			listener.CurrentDateTimeProvider = () => new DateTime(2001, 1, 1);

			t = session.Get<InheritedThing>(1L);
			t.SomeText = "bb";
			session.Update(t);

			session.Flush();
			session.Clear();

			t = session.Get<InheritedThing>(1L);

			Assert.Equal(new DateTime(2001, 1, 1), t.LastModified);
		}

		[Fact]
		public void InheritedThing_LastModified_Should_BeSetOnImplicitUpdate()
		{
			var t = new InheritedThing { Id = 1, SomeText = "aa" };
			session.Save(t);

			session.Flush();
			session.Clear();

			listener.CurrentDateTimeProvider = () => new DateTime(2001, 1, 1);

			t = session.Get<InheritedThing>(1L);
			t.SomeText = "bb";

			session.Flush();
			session.Clear();

			t = session.Get<InheritedThing>(1L);

			Assert.Equal(new DateTime(2001, 1, 1), t.LastModified);
		}

        [Fact]
        public void JoinedThing_LastModified_Should_BeSetOnJoinedObject()
        {
            var t = new SomeThingJoined()
            {
                SomeThingId = 1,
                SomeThingValue = "val1",
                SomeThingJoinValue = "val2"
            };

            session.Save(t);

            session.Flush();
            session.Clear();

            listener.CurrentDateTimeProvider = () => new DateTime(2001, 1, 1);

            var x = session.Get<SomeThingJoined>(1L);
            x.SomeThingValue = "val1change";

            session.Update(x);

            session.Flush();
            session.Clear();

            var y = session.Get<SomeThingJoined>(1L);

            Assert.Equal(new DateTime(2001, 1, 1), y.LastModified);
        }
    }
}
