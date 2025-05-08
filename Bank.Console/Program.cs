using System;
using System.Collections.Generic;

class Program
{
    private static WarehouseCommandHandler _commandHandler;
    private static IReadDatabase<WarehouseViewModel> _readDatabase;
    private static IEventStore _eventStore;

    static void Main(string[] args)
    {
        Initialize();
        RunMainLoop();
    }

    static void Initialize()
    {
        // Initialize the in-memory databases and command handler
        _readDatabase = new WarehouseReadInMemory();
        _eventStore = new WarehouseWriteInMemory();
        _commandHandler = new WarehouseCommandHandler(_readDatabase, _eventStore);

        Console.WriteLine("Warehouse Management System initialized.");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
        Console.Clear();
    }

    static void RunMainLoop()
    {
        bool exit = false;
        while (!exit)
        {
            DisplayMenu();
            var option = Console.ReadLine();

            try
            {
                switch (option)
                {
                    case "1":
                        CreateWarehouse();
                        break;
                    case "2":
                        RemoveWarehouse();
                        break;
                    case "3":
                        ActivateWarehouse();
                        break;
                    case "4":
                        DeactivateWarehouse();
                        break;
                    case "5":
                        AddStock();
                        break;
                    case "6":
                        RemoveStock();
                        break;
                    case "7":
                        ListWarehouses();
                        break;
                    case "8":
                        exit = true;
                        Console.WriteLine("Exiting application. Goodbye!");
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Details: {ex.InnerException.Message}");
                }
            }

            if (!exit)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }

    static void DisplayMenu()
    {
        Console.WriteLine("===== Warehouse Management System =====");
        Console.WriteLine("1. Create Warehouse");
        Console.WriteLine("2. Remove Warehouse");
        Console.WriteLine("3. Activate Warehouse");
        Console.WriteLine("4. Deactivate Warehouse");
        Console.WriteLine("5. Add Stock");
        Console.WriteLine("6. Remove Stock");
        Console.WriteLine("7. List All Warehouses");
        Console.WriteLine("8. Exit");
        Console.WriteLine("=======================================");
        Console.Write("Select an option: ");
    }

    static void CreateWarehouse()
    {
        Console.WriteLine("\n=== Create Warehouse ===");

        Console.Write("Enter Warehouse Name: ");
        string name = Console.ReadLine() ?? string.Empty;

        Console.Write("Enter Warehouse Location: ");
        string location = Console.ReadLine() ?? string.Empty;

        Console.Write("Enter Initial Stock: ");
        if (!int.TryParse(Console.ReadLine(), out int stock))
        {
            Console.WriteLine("Invalid stock value. Using default value 0.");
            stock = 0;
        }

        try
        {
            var command = new CreateWarehouseCommand
            {
                Id = Guid.NewGuid(),
                Name = name,
                Location = location,
                Stock = stock,
                IsActive = true
            };

            _commandHandler.Handle(command);
            Console.WriteLine($"Warehouse created successfully with ID: {command.Id}");
            Console.WriteLine("Please write down this ID for future reference.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating warehouse: {ex.Message}");
            throw;
        }
    }

    static void RemoveWarehouse()
    {
        Console.WriteLine("\n=== Remove Warehouse ===");
        Guid id = GetWarehouseId();
        if (id == Guid.Empty) return;

        try
        {
            var command = new RemoveWarehouseCommand
            {
                Id = id
            };

            _commandHandler.Handle(command);
            Console.WriteLine("Warehouse removed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing warehouse: {ex.Message}");
            throw;
        }
    }

    static void ActivateWarehouse()
    {
        Console.WriteLine("\n=== Activate Warehouse ===");
        Guid id = GetWarehouseId();
        if (id == Guid.Empty) return;

        try
        {
            var command = new ActiveWarehouseCommand
            {
                Id = id,
                IsActive = true
            };

            _commandHandler.Handle(command);
            Console.WriteLine("Warehouse activated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error activating warehouse: {ex.Message}");
            throw;
        }
    }

    static void DeactivateWarehouse()
    {
        Console.WriteLine("\n=== Deactivate Warehouse ===");
        Guid id = GetWarehouseId();
        if (id == Guid.Empty) return;

        try
        {
            var command = new InActiveWarehouseCommand
            {
                Id = id,
                IsActive = false
            };

            _commandHandler.Handle(command);
            Console.WriteLine("Warehouse deactivated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deactivating warehouse: {ex.Message}");
            throw;
        }
    }

    static void AddStock()
    {
        Console.WriteLine("\n=== Add Stock ===");
        Guid id = GetWarehouseId();
        if (id == Guid.Empty) return;

        Console.Write("Enter stock amount to add: ");
        if (!int.TryParse(Console.ReadLine(), out int stock) || stock <= 0)
        {
            Console.WriteLine("Invalid stock value. Stock must be greater than zero.");
            return;
        }

        try
        {
            var command = new AddStockWarehouseCommand
            {
                Id = id,
                Stock = stock
            };

            _commandHandler.Handle(command);
            Console.WriteLine($"Added {stock} items to warehouse stock successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding stock: {ex.Message}");
            throw;
        }
    }

    static void RemoveStock()
    {
        Console.WriteLine("\n=== Remove Stock ===");
        Guid id = GetWarehouseId();
        if (id == Guid.Empty) return;

        Console.Write("Enter stock amount to remove: ");
        if (!int.TryParse(Console.ReadLine(), out int stock) || stock <= 0)
        {
            Console.WriteLine("Invalid stock value. Stock must be greater than zero.");
            return;
        }

        try
        {
            var command = new RemoveStockWarehouseCommand
            {
                Id = id,
                Stock = stock
            };

            _commandHandler.Handle(command);
            Console.WriteLine($"Removed {stock} items from warehouse stock successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing stock: {ex.Message}");
            throw;
        }
    }

    static void ListWarehouses()
    {
        Console.WriteLine("\n=== Warehouse List ===");

        try
        {
            var warehouses = _readDatabase.GetAll();
            int count = 0;

            foreach (var warehouse in warehouses)
            {
                count++;
                Console.WriteLine($"ID: {warehouse.WarehouseId}");
                Console.WriteLine($"Name: {warehouse.Name}");
                Console.WriteLine($"Location: {warehouse.Location}");
                Console.WriteLine($"Stock: {warehouse.Stock}");
                Console.WriteLine($"Status: {(warehouse.IsActive ? "Active" : "Inactive")}");
                Console.WriteLine(new string('-', 40));
            }

            if (count == 0)
            {
                Console.WriteLine("No warehouses found.");
            }
            else
            {
                Console.WriteLine($"Total warehouses: {count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing warehouses: {ex.Message}");
            throw;
        }
    }

    static Guid GetWarehouseId()
    {
        try
        {
            // First show available warehouses to help user select
            var warehouses = _readDatabase.GetAll();
            if (!warehouses.Any())
            {
                Console.WriteLine("No warehouses available.");
                return Guid.Empty;
            }

            Console.WriteLine("Available warehouses:");
            foreach (var warehouse in warehouses)
            {
                Console.WriteLine($"{warehouse.WarehouseId} - {warehouse.Name} ({(warehouse.IsActive ? "Active" : "Inactive")})");
            }

            Console.Write("\nEnter Warehouse ID: ");
            if (Guid.TryParse(Console.ReadLine(), out Guid id))
            {
                return id;
            }

            Console.WriteLine("Invalid ID format.");
            return Guid.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving warehouses: {ex.Message}");
            return Guid.Empty;
        }
    }
}