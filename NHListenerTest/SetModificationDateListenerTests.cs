using System;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using Xunit;

namespace NHListenerTest
{
	public class SetModificationDateIntegrationTests : IDisposable
	{
		private readonly ISessionFactory sessionFactory;
		private readonly ISession session;
		private SetModificationTimeFlushEntityEventListener listener;
		private readonly DateTime defaultDate = new DateTime(2000, 1, 1);

		public SetModificationDateIntegrationTests()
		{
			listener =	new SetModificationTimeFlushEntityEventListener()
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
				})
				.ExposeConfiguration(cfg =>
				{
					listener.Register(cfg);
					config = cfg;
					cfg.SetProperty("show_sql", "true");
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
	}
}
