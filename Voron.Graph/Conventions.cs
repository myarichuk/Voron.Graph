using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph
{
    //this would probably have much more in it than IIdGenerator by the end of the day :)
    public class Conventions
    {
        public Func<long> GetNextNodeKey { get; set; }
    }
}
