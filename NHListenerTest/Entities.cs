using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NHListenerTest
{
	public class Thing : IAuditable
	{
		public virtual long Id { get; set; }
		public virtual DateTime LastModified { get; set; }
        public virtual User ModifiedBy { get; set; }
	    private IList<RelatedThing> relatedThings = new List<RelatedThing>();
		public virtual IEnumerable<RelatedThing> RelatedThings { get { return relatedThings; } }

		public Thing()
		{
			AddRelatedThing(1, "related 1");
		}

		public virtual RelatedThing AddRelatedThing(long id, string name)
		{
			var t = new RelatedThing { Id = id, Name = name, Parent = this };
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

    public class User
    {
        public virtual long Id { get; set; }
        public virtual string Name { get; set; }
    }
}
