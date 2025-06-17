using System;
using System.Collections.Generic;

namespace TestTask
{
    public struct ShipmentRecord
    {
        public string TruckType { get; set; }
        public Dictionary<string, int> ProductsCount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
