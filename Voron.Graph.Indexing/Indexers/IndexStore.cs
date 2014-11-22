using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Indexing.Indexers
{
    public class IndexStore
    {
	    private readonly StorageEnvironment _storage;
	    private readonly string _storageName;


	    public IndexStore(StorageEnvironment storage, string storageName)
	    {
		    if (storage == null) throw new ArgumentNullException("storage");
		    _storage = storage;
		    _storageName = storageName;
	    }
    }
}
