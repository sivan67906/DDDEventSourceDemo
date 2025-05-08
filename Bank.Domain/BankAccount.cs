namespace Bank.Domain;

public abstract class BaseEntity<TId>
{
    public required TId Id { get; set; }
}
#region BaseEntity
public sealed class BankAccount : BaseEntity<Guid>
{
    public string? Name { get; private set; }
    public int AccountNumber { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string? IFSCCode { get; private set; }
    public int Balance { get; private set; }
    public bool IsActive { get; private set; }
}
#endregion

#region BaseCommand
public sealed class BaseBankAccountCommand : BaseEntity<Guid>
{

}

#endregion

#region Event

public abstract class BaseEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime TimeStamp { get; set; }
    public Guid Id { get; set; }
}
public sealed class CreatedBankAccountEvent : BaseEvent
{
    public string? Name { get; private set; }
    public int AccountNumber { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string? IFSCCode { get; private set; }
    public int Balance { get; private set; }
}
public sealed class DeletedBankAccountEvent : BaseEvent
{

}
public sealed class WithdrawledBankAccountEvent : BaseEvent
{
    public int Balance { get; private set; }
}
public sealed class DepositedBankAccountEvent : BaseEvent
{
    public int Balance { get; private set; }
}
public sealed class ActivatedBankAccountEvent : BaseEvent
{
    public bool IsActive { get; private set; }
}
public sealed class DeactivatedBankAccountEvent : BaseEvent
{
    public bool IsActive { get; private set; }
}
#endregion

#region Aggregate
public class BankAccountAggregate
{
    public Guid Id { get; set; }
    public string Name { get; private set; }
    public int AccountNumber { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string IFSCCode { get; private set; }
    public int Balance { get; private set; }
    public bool IsActive { get; private set; }

    public BankAccountAggregate()
    {

    }

    public void Apply(CreatedBankAccountEvent e)
    {
        Id = e.Id;
        Name = e.Name;
        AccountNumber = e.AccountNumber;
        DateOfBirth = e.DateOfBirth;
        IFSCCode = e.IFSCCode;
        Balance = e.Balance;
    }
    //public void Apply(DeletedBankAccountEvent e)
    //{
    //    Id = e.Id;
    //    IsActive = false;
    //}
    public void Apply(WithdrawledBankAccountEvent e)
    {
        if (Id == Guid.Empty && IsActive) throw new ArgumentException("No data found or The data is already in active condition");
        Balance -= e.Balance;
    }
    public void Apply(DepositedBankAccountEvent e)
    {
        if (e.Balance < 0 || Balance < 0) throw new ArgumentException("Invalid Balance");
        Balance += e.Balance;
    }
    public void Apply(ActivatedBankAccountEvent e)
    {
        if (Id == Guid.Empty && IsActive) throw new ArgumentException("No data found or The data is already in Active condition");
        IsActive = true;
    }
    public void Apply(DeactivatedBankAccountEvent e)
    {
        if (Id == Guid.Empty && !IsActive) throw new ArgumentException("No data found or The data is already in Inactive condition");
        IsActive = false;
    }
    public void LoadHistory(IEnumerable<BaseEvent> events)
    {
        foreach (BaseEvent @event in events)
        {
            ApplyEvent(@event);
        }
    }

    public void ApplyEvent(BaseEvent e)
    {
        switch (e)
        {
            case CreatedBankAccountEvent createdBankAccountEvent:
                Apply(createdBankAccountEvent);
                break;
            //case DeletedBankAccountEvent deletedBankAccountEvent:
            //    Apply(deletedBankAccountEvent);
            //    break;
            case WithdrawledBankAccountEvent withdrawledBankAccountEvent:
                Apply(withdrawledBankAccountEvent);
                break;
            case DepositedBankAccountEvent depositedBankAccountEvent:
                Apply(depositedBankAccountEvent);
                break;
            case ActivatedBankAccountEvent activatedBankAccountEvent:
                Apply(activatedBankAccountEvent);
                break;
            case DeactivatedBankAccountEvent deactivatedBankAccountEvent:
                Apply(deactivatedBankAccountEvent);
                break;
            default:
                break;
        }
    }
}


#endregion

#region EventStore - Write
public interface IEventStore
{
    public void SaveEvent(Guid aggregateId, IEnumerable<BaseEvent> baseEvents);
    public IEnumerable<BaseEvent> LoadEvent(Guid aggregateId);
}

public class BankAccountEventImplementation : IEventStore
{
    private readonly Dictionary<Guid, List<BaseEvent>> _inMemory = [];
    public IEnumerable<BaseEvent> LoadEvent(Guid aggregateId)
    {
        if (!_inMemory.TryGetValue(aggregateId, out List<BaseEvent>? events))
            throw new KeyNotFoundException("No event found");
        return events;
    }

    public void SaveEvent(Guid aggregateId, IEnumerable<BaseEvent> baseEvents)
    {
        if (!_inMemory.ContainsKey(aggregateId))
            _inMemory[aggregateId] = [];
        _inMemory[aggregateId].AddRange(baseEvents);
    }
}
#endregion

#region EventStore - Read
public class BankAccountViewModel
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public int AccountNumber { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public required string IFSCCode { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
}

public interface IBankAccountRetrieval
{
    public void Add(BankAccountViewModel bankAccountViewModel);
    public void Delete(Guid id);
    public void Update(BankAccountViewModel bankAccountViewModel);
    public List<BankAccountViewModel> GetAll();
    public BankAccountViewModel GetById(Guid id);
}

public class BankAccountRetrieveImplementation : IBankAccountRetrieval
{
    private readonly Dictionary<Guid, BankAccountViewModel> _inMemory = [];

    public void Add(BankAccountViewModel bankAccountViewModel)
    {
        if (bankAccountViewModel == null)
            throw new ArgumentNullException("Input data is null or invalid");
        if (_inMemory.ContainsKey(bankAccountViewModel.Id))
            throw new InvalidOperationException("Bank account id already exists");

        _inMemory.Add(bankAccountViewModel.Id, bankAccountViewModel);
    }

    public void Delete(Guid id)
    {
        if (!_inMemory.ContainsKey(id))
            throw new KeyNotFoundException("Bank account detail not found");

        _inMemory.Remove(id);
    }

    public List<BankAccountViewModel> GetAll()
    {
        return _inMemory.Values.ToList();
    }

    public BankAccountViewModel GetById(Guid id)
    {
        if (!_inMemory.ContainsKey(id))
            throw new KeyNotFoundException("Bank account detail not found");

        return _inMemory[id];
    }

    public void Update(BankAccountViewModel bankAccountViewModel)
    {
        if (bankAccountViewModel == null)
            throw new ArgumentNullException("Input data is null or invalid");
        if (!_inMemory.ContainsKey(bankAccountViewModel.Id))
            throw new KeyNotFoundException("Bank account detail not found");

        _inMemory[bankAccountViewModel.Id] = bankAccountViewModel;
    }



}
#endregion