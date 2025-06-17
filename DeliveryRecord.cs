using System;

namespace TestTask
{
    public struct DeliveryRecord
    {
        public string FactoryName { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
