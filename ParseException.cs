namespace Funnyppt.Net;
public class ParseException : Exception {
    public ParseException() {
    }

    public ParseException(string? message) : base(message) {
    }

    public ParseException(string? message, Exception? innerException) : base(message, innerException) {
    }
}
