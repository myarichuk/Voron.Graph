using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voron.Graph.Impl;
using Voron.Graph.Interfaces;

namespace Voron.Graph
{
    public class HiLoIdGenerator : IIdGenerator
    {
        private readonly IHeaderAccessor _headerAccessor;
        private long _loValue;
        private long _hiValue;
        private readonly object _syncObject = new object();

        public HiLoIdGenerator(IHeaderAccessor headerAccessor)
        {
            _headerAccessor = headerAccessor;
            _hiValue = GetNextHiAndIncrement();
            _loValue = 0;
        }

        public long NextId()
        {
            lock (_syncObject)
            {
                if ((_loValue + 1) >= Constants.HiLoRangeCapacity)
                {
                    _hiValue = GetNextHiAndIncrement();
                    _loValue = 0;
                }

                return ((_hiValue - 1) * Constants.HiLoRangeCapacity) + (++_loValue);
            }
        }

        private long GetNextHiAndIncrement()
        {           
            var nextHi = _headerAccessor.Get(header => header.NextHi);
            Interlocked.Increment(ref nextHi);
            _headerAccessor.Modify(header =>
            {
                header.NextHi = nextHi;
                return header;
            });

            return nextHi;
        }
    }
}
