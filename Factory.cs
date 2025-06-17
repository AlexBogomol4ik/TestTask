using System;
using System.Collections.Generic;
using System.Threading;

namespace TestTask
{
    public class Factory
    {
        public string Name { get; }
        public double ProductionRate { get; }
        public int BaseProduction { get; }
        private readonly Warehouse _warehouse;
        private readonly CancellationToken _ct;
        private readonly Random _random = new Random();
        private readonly string[] _packagingTypes = { "Cardboard", "Plastic", "Wooden", "Metal" };

        public Factory(string name, double rate, int baseProduction, Warehouse warehouse, CancellationToken ct)
        {
            Name = name;
            ProductionRate = rate;
            BaseProduction = baseProduction;
            _warehouse = warehouse;
            _ct = ct;
        }

        public void Run()
        {
            while (!_ct.IsCancellationRequested)
            {
                int production = (int)(BaseProduction * ProductionRate);
                var products = new List<Product>(production);

                for (int i = 0; i < production; i++)
                {
                    products.Add(new Product
                    {
                        Name = Name.ToLower(),
                        Weight = _random.NextDouble() * 10 + 0.5,
                        PackagingType = _packagingTypes[_random.Next(_packagingTypes.Length)],
                        FactorySource = Name
                    });
                }

                _warehouse.AddProducts(products);
                Thread.Sleep(100);
            }
        }
    }
}
