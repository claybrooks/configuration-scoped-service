namespace MBL.ConfigurationScopedService;

public enum SwapStrategies
{
    Block,
    NoBlock,
}

public class ConfigurationScopeRuntimeOptions
{
    /// <summary>
    /// Developer feature to find disposal bugs within the application.  This should be turned off in production.  Will throw if any services are un-disposed when the root service provider scope lifetime ends.
    /// </summary>
    public bool ThrowOnDispose { get; set; } = false;

    /// <summary>
    /// The amount of time a service has to dispose before a warning log is produced.
    /// </summary>
    public TimeSpan? WarnTime { get; set; } = null;

    /// <summary>
    /// Strategies available for how to handle loading new services and presenting new ones to consumers
    /// </summary>
    public bool BlockOnSwap { get; set; } = false;
}