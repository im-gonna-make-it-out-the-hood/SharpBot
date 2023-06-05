namespace SharpBot.Systems.ProxyChecker;

public struct ProxyResult {
    public string host;
    public ushort port;
    public bool isSuccess;

    public ProxyResult(string host, ushort port, bool isSuccess) {
        this.host = host;
        this.port = port;
        this.isSuccess = isSuccess;
    }

    public ProxyResult(string host, int port, bool isSuccess) : this(host, (ushort)port, isSuccess) { // Call ctor.
        if (port > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be less than 65535!");
    }
}