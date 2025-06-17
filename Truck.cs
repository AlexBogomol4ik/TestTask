namespace TestTask
{
    public struct Truck
    {
        public string Type { get; }
        public int Capacity { get; }

        public Truck(string type, int capacity)
        {
            Type = type;
            Capacity = capacity;
        }
    }
}
