using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voron.Graph.Impl;
using Voron.Graph.Interfaces;

namespace Voron.Graph
{
    public class HiLoIdGenerator : IIdGenerator
    {
        private readonly IHeaderAccessor _headerAccessor;
        private long loValue;
        private long hiValue;

        public HiLoIdGenerator(IHeaderAccessor headerAccessor)
        {
            _headerAccessor = headerAccessor;
            hiValue = GetNextHiAndIncrement();
            loValue = 0;
        }

        public long NextId()
        {
            if((loValue + 1) >= Constants.HiLoRangeCapacity)
            {
                hiValue = GetNextHiAndIncrement();
                loValue = 0;
            }

            return ((hiValue - 1) * Constants.HiLoRangeCapacity) + (++loValue);
        }

        private long GetNextHiAndIncrement()
        {           
            var nextHi = _headerAccessor.Get(header => header.NextHi) + 1;
            _headerAccessor.Modify(header =>
            {
                header.NextHi = nextHi;
                return header;
            });

            return nextHi;
        }
    }
}
