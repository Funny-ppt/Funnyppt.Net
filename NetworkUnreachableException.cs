namespace Funnyppt.Net;
public class NetworkUnreachableException : Exception {
    public NetworkUnreachableException(string? message) : base(message) {
    }
}
