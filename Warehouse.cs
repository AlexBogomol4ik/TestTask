using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestTask
{
    public class Warehouse
    {
        private readonly int _capacity;
        private readonly ConcurrentQueue<Product> _storage = new ConcurrentQueue<Product>();
        private readonly object _shippingLock = new object();
        private volatile bool _shippingActive = false;
        private readonly List<Truck> _trucks;
        private readonly ConcurrentBag<DeliveryRecord> _deliveryLog = new ConcurrentBag<DeliveryRecord>();
        private readonly ConcurrentBag<ShipmentRecord> _shipmentLog = new ConcurrentBag<ShipmentRecord>();
        private int _totalProduced;
        private int _totalShipped;

        public Warehouse(int capacity, List<Truck> trucks)
        {
            _capacity = capacity;
            _trucks = trucks;
        }

        public void AddProducts(List<Product> products)
        {
            lock (_shippingLock)
            {
                while (_storage.Count + products.Count > _capacity)
                {
                    Monitor.Wait(_shippingLock);
                }

                foreach (var product in products)
                {
                    _storage.Enqueue(product);
                }

                _totalProduced += products.Count;
                _deliveryLog.Add(new DeliveryRecord
                {
                    FactoryName = products[0].FactorySource,
                    ProductName = products[0].Name,
                    Quantity = products.Count,
                    Timestamp = DateTime.Now
                });

                double fillPercentage = (double)_storage.Count / _capacity;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {products[0].FactorySource} доставил {products.Count} едениц | Остаток на складе: {_storage.Count}/{_capacity} ({fillPercentage:P})");

                if (fillPercentage >= 0.95 && !_shippingActive)
                {
                    Console.WriteLine($"\n!!! Склад {fillPercentage:P} заполнен - начало отгрузки !!!\n");
                    _shippingActive = true;
                    Task.Run(StartShipping);
                }
            }
        }

        private void StartShipping()
        {
            Console.WriteLine($"Процесс доставки начался в {DateTime.Now:HH:mm:ss}");

            while (true)
            {
                double fillPercentage;
                lock (_shippingLock)
                {
                    fillPercentage = (double)_storage.Count / _capacity;
                    if (fillPercentage < 0.7)
                    {
                        Console.WriteLine($"\n!!! Склад {fillPercentage:P} пустой - остановка отгрузки !!!\n");
                        _shippingActive = false;

                        if (fillPercentage >= 0.95)
                        {
                            Console.WriteLine($"!!! Склад по прежнему {fillPercentage:P} полный - перезапуск процесса отгрузок !!!");
                            _shippingActive = true;
                        }
                        else
                        {
                            Monitor.PulseAll(_shippingLock);
                            return;
                        }
                    }
                }

                var truck = _trucks[new Random().Next(_trucks.Count)];
                var shipment = RetrieveProducts(truck.Capacity);

                if (shipment.Count == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }

                _totalShipped += shipment.Count;

                var productGroups = shipment
                    .GroupBy(p => p.FactorySource)
                    .ToDictionary(g => g.Key, g => g.Count());

                _shipmentLog.Add(new ShipmentRecord
                {
                    TruckType = truck.Type,
                    ProductsCount = productGroups,
                    Timestamp = DateTime.Now
                });

                Console.Write($"[{DateTime.Now:HH:mm:ss.fff}] {truck.Type} грузовики вместимостью: {shipment.Count}: ");
                foreach (var group in productGroups)
                {
                    Console.Write($"{group.Key}-{group.Value} ");
                }
                Console.WriteLine($"| Количестов: {_storage.Count}/{_capacity} ({(double)_storage.Count / _capacity:P})");

                Thread.Sleep(50);
            }
        }

        private List<Product> RetrieveProducts(int count)
        {
            var products = new List<Product>();
            lock (_shippingLock)
            {
                for (int i = 0; i < count && _storage.TryDequeue(out var product); i++)
                {
                    products.Add(product);
                }
                Monitor.PulseAll(_shippingLock);
            }
            return products;
        }

        public IEnumerable<DeliveryRecord> GetDeliveryLog() => _deliveryLog;
        public IEnumerable<ShipmentRecord> GetShipmentLog() => _shipmentLog;
        public int CurrentStock => _storage.Count;
        public int TotalProduced => _totalProduced;
        public int TotalShipped => _totalShipped;
    }
}
