namespace MoneyTracker.Services;

// Gives out ids 1, 2, 3...; ids are never reused, even after a delete.
// TransactionService and CategoryService each have their own IdGenerator.
public class IdGenerator
{
    private int _nextId = 1;

    // The id that will be given next (saved to the file)
    public int NextId => _nextId;

    // Return the next id and move the counter
    public int GetNextId()
    {
        int id = _nextId;
        _nextId++;
        return id;
    }

    // After loading from the file: never below the biggest existing id + 1, even if the file was edited by hand
    public void Restore(int savedNextId, int maxExistingId)
    {
        _nextId = Math.Max(savedNextId, maxExistingId + 1);
    }
}
