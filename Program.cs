using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestTask
{
    internal class Program
    {
        public static void Main()
        {
            Console.WriteLine("#Начало симуляции#");

            const int n = 300;
            const int M = 50;

            var factoryRates = new[]
            {
                ("A", 1.0),
                ("B", 1.1),
                ("C", 1.2),
                ("D", 0.9),
                ("E", 1.3)
            };

            double totalHourlyProduction = factoryRates.Sum(f => n * f.Item2);
            int warehouseCapacity = (int)(M * totalHourlyProduction);

            Console.WriteLine($"Производственная мощность: {totalHourlyProduction}/час");
            Console.WriteLine($"Вместимость склада: {warehouseCapacity} единиц");

            var trucks = new List<Truck>
            {
                new Truck("Small", 200),
                new Truck("Medium", 500),
                new Truck("Large", 1000),
                new Truck("Mega", 2000)
            };

            Console.WriteLine("Available trucks:");
            foreach (var truck in trucks)
            {
                Console.WriteLine($"- {truck.Type} ({truck.Capacity} единиц)");
            }

            var warehouse = new Warehouse(warehouseCapacity, trucks);
            var cts = new CancellationTokenSource();

            var factories = factoryRates.Select(f =>
                new Factory(f.Item1, f.Item2, n, warehouse, cts.Token)).ToList();

            var factoryTasks = factories.Select(f => Task.Run(() => f.Run())).ToList();

            Task.Run(() => MonitorSystem(warehouse, warehouseCapacity, cts.Token));

            Thread.Sleep(120000);
            cts.Cancel();
            Task.WaitAll(factoryTasks.ToArray());

            PrintResults(warehouse);

            Console.ReadLine();
        }

        private static void MonitorSystem(Warehouse warehouse, int capacity, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                int currentStock = warehouse.CurrentStock;
                double percentage = (double)currentStock / capacity;

                ConsoleColor color = ConsoleColor.Green;

                switch (percentage)
                {
                    case 0.95:
                        {
                            color = ConsoleColor.Red;
                            break;
                        }
                    case 0.8:
                        {
                            color = ConsoleColor.Yellow;
                            break;
                        }
                }

                Console.ForegroundColor = color;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] cклад: {currentStock}/{capacity} ({percentage:P})");
                Console.ResetColor();

                Thread.Sleep(1000);

                Console.ReadLine();
            }
        }

        private static void PrintResults(Warehouse warehouse)
        {
            Console.WriteLine("\nИтоговый отчет");
            Console.WriteLine($"Всего произведено: {warehouse.TotalProduced}");
            Console.WriteLine($"Всего отгружено: {warehouse.TotalShipped}");
            Console.WriteLine($"Текущий запас: {warehouse.CurrentStock}");

            Console.WriteLine("\nСтатистика отгрузки");
            var shipments = warehouse.GetShipmentLog().ToList();

            Console.WriteLine($"Общий объем отгрузок: {shipments.Count}");

            if (shipments.Count == 0)
            {
                Console.WriteLine("Отгрузок не зарегистрировано!");
                return;
            }

            var groupedByTruck = shipments.GroupBy(s => s.TruckType);

            foreach (var group in groupedByTruck)
            {
                int count = group.Count();
                double avgProducts = group.Average(s => s.ProductsCount.Values.Sum());

                Console.WriteLine($"\nГрузовик '{group.Key}':");
                Console.WriteLine($"  Поездки: {count}");
                Console.WriteLine($"  Среднее количество продуктов за поездку: {avgProducts:F1}");

                var totalProducts = new Dictionary<string, int>();
                foreach (var shipment in group)
                {
                    foreach (var kvp in shipment.ProductsCount)
                    {
                        string factory = kvp.Key;
                        int quantity = kvp.Value;

                        if (!totalProducts.ContainsKey(factory))
                            totalProducts[factory] = 0;
                        totalProducts[factory] += quantity;
                    }
                }

                int grandTotal = totalProducts.Values.Sum();
                Console.WriteLine("  Состав продукта:");
                foreach (var kvp in totalProducts)
                {
                    string factory = kvp.Key;
                    int quantity = kvp.Value;
                    double percentage = (double)quantity / grandTotal * 100;
                    Console.WriteLine($"    {factory}: {percentage:F1}% ({quantity} единицы)");
                }
            }

            Console.ReadLine();
        }
    }
}
