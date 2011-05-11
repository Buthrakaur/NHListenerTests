using FluentNHibernate.Mapping;

namespace NHListenerTest
{
	public class ThingMap : ClassMap<Thing>
	{
		public ThingMap()
		{
			Id(x => x.Id).GeneratedBy.Assigned();
			Map(x => x.LastModified);
		    References(x => x.LastModifiedBy);
			HasMany(x => x.RelatedThings)
				.Cascade.All()
                .Inverse()
				.Access.ReadOnlyPropertyThroughCamelCaseField();
		}
	}

    public class UserMap : ClassMap<User>
    {
        public UserMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Name);
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
}
