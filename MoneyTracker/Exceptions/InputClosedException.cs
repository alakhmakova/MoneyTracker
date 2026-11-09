namespace MoneyTracker.Exceptions;

// Thrown when console input is closed (ReadLine returns null)
public class InputClosedException : Exception
{
    public InputClosedException()
        : base("Input stream was closed. No more input can be read.") { }

    public InputClosedException(string message)
        : base(message) { }

    public InputClosedException(string message, Exception innerException)
        : base(message, innerException) { }
}
