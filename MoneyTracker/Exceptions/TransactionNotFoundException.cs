namespace MoneyTracker.Exceptions;

// Thrown when no transaction matches an id or a search
public class TransactionNotFoundException : Exception
{
    public TransactionNotFoundException()
        : base("Transaction not found.") { }

    public TransactionNotFoundException(string message)
        : base(message) { }

    public TransactionNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}
