using System;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using log4net.Config;
using NHibernate;
using NHibernate.Cfg.Loquacious;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using Xunit;

namespace NHListenerTest
{
    public class FakeCurrentPrincipalProvider : ICurrentPrincipalIdProvider
    {
        public long GetCurrentPrincipalId()
        {
            return 1;
        }
    }

    public class SetModifiedByTests : IDisposable
    {
        private readonly ISessionFactory sessionFactory;
        private readonly ISession session;
        private SetModifiedByListener listener;
        private readonly DateTime defaultDate = new DateTime(2000, 1, 1);

        public SetModifiedByTests()
        {
            XmlConfigurator.Configure();
            listener = new SetModifiedByListener(new FakeCurrentPrincipalProvider());

            Configuration config = null;
            sessionFactory = Fluently.Configure()
                .Database(SQLiteConfiguration.Standard.InMemory)
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ThingMap>())
                .ExposeConfiguration(cfg =>
                {
                    listener.Register(cfg);
                    config = cfg;
                    cfg.DataBaseIntegration(db => db.LogFormatedSql = true);
                })
                .BuildSessionFactory();

            session = sessionFactory.OpenSession();

            using (var tx = session.BeginTransaction())
            {
                new SchemaExport(config).Execute(false, true, false, session.Connection, null);
                tx.Commit();
            }
            using(var tx = session.BeginTransaction())
            {
                session.Save(new User { Id = 1, Name = "Tux" });
                tx.Commit();
            }
        }

        public void Dispose()
        {
            session.Dispose();
            sessionFactory.Dispose();
        }


        [Fact]
        public void LastModifiedBy_Should_BeSetOnInsert()
        {
            using (var tx = session.BeginTransaction())
            {
                var t = new Thing { Id = 1 };
                session.SaveOrUpdate(t);
                tx.Commit();
                Assert.Equal(1, t.ModifiedBy.Id);
            }
        }

    }
}