using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Event;
using NHibernate.Tool.hbm2ddl;
using Xunit;

namespace NHListenerTest
{
	public class SetModificationDateIntegrationTests : IDisposable
	{
		private readonly ISessionFactory sessionFactory;
		private readonly ISession session;
		private IModificationTimeListener listener;

		public class Thing : ITrackModificationDate
		{
			public virtual long Id { get; set; }
			public virtual DateTime LastModified { get; set; }
			private IList<RelatedThing> relatedThings = new List<RelatedThing>();
			public virtual IEnumerable<RelatedThing> RelatedThings { get { return relatedThings; } }

			public Thing()
			{
				AddRelatedThing(1, "related 1");
			}

			public virtual RelatedThing AddRelatedThing(long id, string name)
			{
				var t = new RelatedThing {Id = id, Name = name, Parent = this};
				relatedThings.Add(t);
				return t;
			}
		}

		public class InheritedThing : Thing
		{
			public virtual string SomeText { get; set; }
		}

		public class RelatedThing
		{
			public virtual long Id { get; set; }
			public virtual string Name { get; set; }
			public virtual Thing Parent { get; set; }
		}

		public class ThingMap : ClassMap<Thing>
		{
			public ThingMap()
			{
				Id(x => x.Id).GeneratedBy.Assigned();
				Map(x => x.LastModified);
				HasMany(x => x.RelatedThings)
					.Cascade.All()
					.Access.ReadOnlyPropertyThroughCamelCaseField();
			}
		}
		
		public class RelatedThingMap : ClassMap<RelatedThing>
		{
			public RelatedThingMap()
			{
				Id(x => x.Id).GeneratedBy.Assigned();
				Map(x => x.Name);
				References(x => x.Parent).Not.Nullable();
			}
		}

		public class InheritedThingMap : SubclassMap<InheritedThing>
		{
			public InheritedThingMap()
			{
				Map(x => x.SomeText);
			}
		}



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
