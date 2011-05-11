using System;

namespace NHListenerTest
{
	public interface IAuditable
	{
		DateTime LastModified { get; set; }
        User ModifiedBy { get; set; }
	}
}
