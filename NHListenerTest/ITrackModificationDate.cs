using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NHListenerTest
{
	public interface ITrackModificationDate
	{
		DateTime LastModified { get; set; }
        User LastModifiedBy { get; set; }
	}

    
}
