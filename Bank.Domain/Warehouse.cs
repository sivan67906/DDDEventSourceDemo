#region Entity
public class BaseWarehouseEntity<TId>
{
    public TId Id { get; set; }
}

public class Warehouse : BaseWarehouseEntity<Guid>
{
    public string Name { get; set; }
    public string Location { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

#endregion

#region Command
public class BaseWarehouseCommand : BaseWarehouseEntity<Guid>
{

}

public class CreateWarehouseCommand : BaseWarehouseCommand
{
    public string Name { get; set; }
    public string Location { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

public class RemoveWarehouseCommand : BaseWarehouseCommand
{

}

public class ActiveWarehouseCommand : BaseWarehouseCommand
{
    public bool IsActive { get; set; }
}

public class InActiveWarehouseCommand : BaseWarehouseCommand
{
    public bool IsActive { get; set; }
}

public class AddStockWarehouseCommand : BaseWarehouseCommand
{
    public int Stock { get; set; }
}

public class RemoveStockWarehouseCommand : BaseWarehouseCommand
{
    public int Stock { get; set; }
}

#endregion

#region Event

public class BaseWarehouseEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime TimeStamp { get; set; }

    public Guid Id { get; set; }
}

public class WarehouseCreatedEvent : BaseWarehouseEvent
{
    public string Name { get; set; }
    public string Location { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

public class WarehouseRemovedEvent : BaseWarehouseEvent
{

}

public class WarehouseActivatedEvent : BaseWarehouseEvent
{
    public bool IsActive { get; set; }
}

public class WarehouseInActivatedEvent : BaseWarehouseEvent
{
    public bool IsActive { get; set; }
}

public class WarehouseStockAddedEvent : BaseWarehouseEvent
{
    public int Stock { get; set; }
}

public class WarehouseStockRemovedEvent : BaseWarehouseEvent
{
    public int Stock { get; set; }
}

#endregion

#region Aggregate

public class WarehouseAggregate
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }

    public WarehouseAggregate()
    {

    }

    public void Apply(WarehouseCreatedEvent e)
    {
        Id = e.Id;
        Name = e.Name;
        Location = e.Location;
        Stock = e.Stock;
        IsActive = true;
    }

    public void Apply(WarehouseRemovedEvent e)
    {
        Id = e.Id;
        IsActive = false;
    }

    public void Apply(WarehouseActivatedEvent e)
    {
        if (IsActive)
            throw new InvalidOperationException("Already Activated");

        IsActive = true;
    }

    public void Apply(WarehouseInActivatedEvent e)
    {
        if (!IsActive) throw new InvalidOperationException("Already In Active");

        IsActive = false;
    }

    public void Apply(WarehouseStockAddedEvent e)
    {
        if (e.Stock < 0) throw new ArgumentOutOfRangeException("Stock must be greater than Zero");

        Stock += e.Stock;
    }

    public void Apply(WarehouseStockRemovedEvent e)
    {
        if (e.Stock < 0) throw new ArgumentOutOfRangeException("Stock must be greater than Zero");

        Stock -= e.Stock;
    }

    public void LoadHistory(IEnumerable<BaseWarehouseEvent> events)
    {
        foreach (var @event in events)
            ApplyEvent(@event);
    }
    public class WarehouseReadInMemory : IReadDatabase<WarehouseViewModel>
    {
        private readonly Dictionary<Guid, WarehouseViewModel> _inMemoryStore = [];

        public void Add(WarehouseViewModel entity)
        {
            _inMemoryStore[entity.WarehouseId] = entity;
        }

        public void Delete(Guid id)
        {
            if (!_inMemoryStore.ContainsKey(id))
                throw new KeyNotFoundException("Warehouse Not Found from Delete");

            _inMemoryStore.Remove(id);
        }

        public IEnumerable<WarehouseViewModel> GetAll()
        {
            return _inMemoryStore.Values;
        }

        public WarehouseViewModel GetById(Guid id)
        {
            if (!_inMemoryStore.TryGetValue(id, out var warehouse))
                throw new DirectoryNotFoundException("No Warehouse Found.");

            return warehouse;
        }

        public void Update(WarehouseViewModel entity)
        {
            if (!_inMemoryStore.ContainsKey(entity.WarehouseId))
                throw new DirectoryNotFoundException("No Warehouse Found for Update!");

            _inMemoryStore[entity.WarehouseId] = entity;
        }
    }

    public void ApplyEvent(BaseWarehouseEvent e)
    {
        switch (e)
        {
            case WarehouseCreatedEvent warehouseCreatedEvent: Apply(warehouseCreatedEvent); break;
            case WarehouseRemovedEvent warehouseRemovedEvent: Apply(warehouseRemovedEvent); break;
            case WarehouseActivatedEvent warehouseActivatedEvent: Apply(warehouseActivatedEvent); break;
            case WarehouseInActivatedEvent warehouseInActivatedEvent: Apply(warehouseInActivatedEvent); break;
            case WarehouseStockAddedEvent warehouseStockAddedEvent: Apply(warehouseStockAddedEvent); break;
            case WarehouseStockRemovedEvent warehouseStockRemovedEvent: Apply(warehouseStockRemovedEvent); break;
        }
    }
}

#endregion

#region Event Store - Write Database

public interface IEventStore
{
    void SaveEvent(Guid aggregateId, IEnumerable<BaseWarehouseEvent> events);
    List<BaseWarehouseEvent> LoadEvents(Guid aggregateId);
}

public class WarehouseWriteInMemory : IEventStore
{
    private readonly Dictionary<Guid, List<BaseWarehouseEvent>> _inMemoryStore = [];

    public void SaveEvent(Guid aggregateId, IEnumerable<BaseWarehouseEvent> events)
    {
        if (!_inMemoryStore.ContainsKey(aggregateId))
            _inMemoryStore[aggregateId] = [];

        _inMemoryStore[aggregateId].AddRange(events);
    }

    public List<BaseWarehouseEvent> LoadEvents(Guid aggregateId)
    {
        if (!_inMemoryStore.TryGetValue(aggregateId, out var events))
            throw new KeyNotFoundException("No Events Found.");

        return events;
    }
}
#endregion

#region Event Retrive - Read Database

public class WarehouseViewModel
{
    public Guid WarehouseId { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

public interface IReadDatabase<T> where T : class
{
    void Add(T entity);
    T GetById(Guid id);
    IEnumerable<T> GetAll();
    void Update(T entity);
    void Delete(Guid id);
}

public class WarehouseReadInMemory : IReadDatabase<WarehouseViewModel>
{
    private readonly Dictionary<Guid, WarehouseViewModel> _inMemoryStore = [];

    public void Add(WarehouseViewModel entity)
    {
        _inMemoryStore[entity.WarehouseId] = entity;
    }

    public void Delete(Guid id)
    {
        if (!_inMemoryStore.ContainsKey(id))
            throw new KeyNotFoundException("Warehouse Not Found from Delete");

        _inMemoryStore.Remove(id);
    }

    public IEnumerable<WarehouseViewModel> GetAll()
    {
        return _inMemoryStore.Values;
    }

    public WarehouseViewModel GetById(Guid id)
    {
        if (!_inMemoryStore.TryGetValue(id, out var warehouse))
            throw new DirectoryNotFoundException("No Warehouse Found.");

        return warehouse;
    }

    public void Update(WarehouseViewModel entity)
    {
        if (!_inMemoryStore.ContainsKey(entity.WarehouseId))
            throw new DirectoryNotFoundException("No Warehouse Found for Update!");

        _inMemoryStore[entity.WarehouseId] = entity;
    }
}

#endregion

#region Command Handler

public class WarehouseCommandHandler
{
    private readonly IReadDatabase<WarehouseViewModel> _readDatabase;
    private readonly IEventStore _writeDatabase;

    public WarehouseCommandHandler(IReadDatabase<WarehouseViewModel> readDatabase, IEventStore writeDatabase)
    {
        _readDatabase = readDatabase;
        _writeDatabase = writeDatabase;
    }

    public void Handle(CreateWarehouseCommand command)
    {
        // Initiate Aggregate
        var aggregate = new WarehouseAggregate();

        // Write Database Mapping
        var createdWarehouseEvent = new WarehouseCreatedEvent
        {
            Id = command.Id,
            Name = command.Name,
            Location = command.Location,
            Stock = command.Stock,
            TimeStamp = DateTime.UtcNow
        };

        aggregate.Apply(createdWarehouseEvent);

        // Save Event
        _writeDatabase.SaveEvent(aggregate.Id, new[] { createdWarehouseEvent });

        // Read Database Mapping
        var viewModel = new WarehouseViewModel
        {
            WarehouseId = aggregate.Id,
            Name = aggregate.Name,
            Location = aggregate.Location,
            Stock = aggregate.Stock,
            IsActive = aggregate.IsActive,
        };

        // Save Event - Read Database
        _readDatabase.Add(viewModel);
    }

    public void Handle(RemoveWarehouseCommand command)
    {
        // Load Events from Storage
        var events = _writeDatabase.LoadEvents(command.Id);

        // Initiate Aggregate
        var aggregate = new WarehouseAggregate();

        // Generate History based on Events
        aggregate.LoadHistory(events);

        // Write Database Mapping
        var removedWarehouseEvent = new WarehouseRemovedEvent
        {
            Id = command.Id,
            TimeStamp = DateTime.UtcNow
        };

        aggregate.Apply(removedWarehouseEvent);

        // Save Event - Write Database
        _writeDatabase.SaveEvent(aggregate.Id, new[] { removedWarehouseEvent });

        // Remove from - Read Database
        _readDatabase.Delete(command.Id);
    }

    public void Handle(ActiveWarehouseCommand command)
    {
        // Load Events from Storage
        var events = _writeDatabase.LoadEvents(command.Id);

        // Initiate Aggregate
        var aggregate = new WarehouseAggregate();

        // Generate History based on Events
        aggregate.LoadHistory(events);

        var activatedWarehouseEvent = new WarehouseActivatedEvent
        {
            Id = command.Id,
            IsActive = true,
            TimeStamp = DateTime.UtcNow
        };

        aggregate.Apply(activatedWarehouseEvent);

        _writeDatabase.SaveEvent(aggregate.Id, new[] { activatedWarehouseEvent });

        var viewModel = _readDatabase.GetById(command.Id);
        viewModel.IsActive = aggregate.IsActive;

        _readDatabase.Update(viewModel);
    }

    public void Handle(InActiveWarehouseCommand command)
    {
        // Load Events from Storage
        var events = _writeDatabase.LoadEvents(command.Id);

        // Initiate Aggregate
        var aggregate = new WarehouseAggregate();

        // Generate History based on Events
        aggregate.LoadHistory(events);

        var inActivatedWarehouseEvent = new WarehouseInActivatedEvent
        {
            Id = command.Id,
            IsActive = false,
            TimeStamp = DateTime.UtcNow
        };

        aggregate.Apply(inActivatedWarehouseEvent);

        _writeDatabase.SaveEvent(aggregate.Id, new[] { inActivatedWarehouseEvent });

        var viewModel = _readDatabase.GetById(command.Id);
        viewModel.IsActive = aggregate.IsActive;

        _readDatabase.Update(viewModel);
    }

    public void Handle(AddStockWarehouseCommand command)
    {
        // Load Events from Storage
        var events = _writeDatabase.LoadEvents(command.Id);

        // Initiate Aggregate
        var aggregate = new WarehouseAggregate();

        // Generate History based on Events
        aggregate.LoadHistory(events);

        var stockAddedWarehouseEvent = new WarehouseStockAddedEvent
        {
            Id = command.Id,
            Stock = command.Stock,
            TimeStamp = DateTime.UtcNow
        };

        aggregate.Apply(stockAddedWarehouseEvent);

        _writeDatabase.SaveEvent(aggregate.Id, new[] { stockAddedWarehouseEvent });

        var viewModel = _readDatabase.GetById(command.Id);
        viewModel.Stock = aggregate.Stock;

        _readDatabase.Update(viewModel);
    }

    public void Handle(RemoveStockWarehouseCommand command)
    {
        // Load Events from Storage
        var events = _writeDatabase.LoadEvents(command.Id);

        // Initiate Aggregate
        var aggregate = new WarehouseAggregate();

        // Generate History based on Events
        aggregate.LoadHistory(events);

        var stockRemovedWarehouseEvent = new WarehouseStockRemovedEvent
        {
            Id = command.Id,
            Stock = command.Stock,
            TimeStamp = DateTime.UtcNow
        };

        aggregate.Apply(stockRemovedWarehouseEvent);

        _writeDatabase.SaveEvent(aggregate.Id, new[] { stockRemovedWarehouseEvent });

        var viewModel = _readDatabase.GetById(command.Id);
        viewModel.Stock = aggregate.Stock;

        _readDatabase.Update(viewModel);
    }
}

#endregion
