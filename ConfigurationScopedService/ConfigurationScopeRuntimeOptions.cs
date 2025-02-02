namespace ConfigurationScopedService;

public class ConfigurationScopeRuntimeOptions
{
    /// <summary>
    /// Developer feature to find disposal bugs within the application.  This should be turned off in production.  Will throw if any services are un-disposed when the root service provider scope lifetime ends.
    /// </summary>
    public bool ThrowOnDispose { get; set; }

    /// <summary>
    /// The amount of time a service has to dispose before a warning log is produced.
    /// </summary>
    public TimeSpan? WarnTime { get; set; }
}