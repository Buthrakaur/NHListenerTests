using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Mapping;

namespace NHListenerTest
{
	public class ThingMap : ClassMap<Thing>
	{
		public ThingMap()
		{
			Id(x => x.Id).GeneratedBy.Assigned();
			Map(x => x.LastModified);
			HasMany(x => x.RelatedThings)
				.Inverse()
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

    public class SomeThingJoinedMap : ClassMap<SomeThingJoined>
    {
        public SomeThingJoinedMap()
        {
            Id(x => x.SomeThingId);
            Map(x => x.SomeThingValue);

            Join("SomeThingJoin", m =>
            {
                m.Fetch.Join();
                m.KeyColumn("SomeThingId");
                m.Map(x => x.SomeThingJoinValue);
                m.Map(x => x.LastModified);
            });
        }
    }
}
