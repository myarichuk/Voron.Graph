using System;

namespace Voron.Graph
{
    //this would probably have much more in it than IIdGenerator by the end of the day :)
    public class Conventions
    {
        public Func<long> GetNextNodeKey { get; set; }
    }
}
