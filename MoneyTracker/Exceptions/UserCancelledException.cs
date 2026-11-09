namespace MoneyTracker.Exceptions;

// Thrown when the user enters q to cancel
public class UserCancelledException : Exception
{
    public UserCancelledException()
        : base("User cancelled the operation.") { }

    public UserCancelledException(string message)
        : base(message) { }

    public UserCancelledException(string message, Exception innerException)
        : base(message, innerException) { }
}
