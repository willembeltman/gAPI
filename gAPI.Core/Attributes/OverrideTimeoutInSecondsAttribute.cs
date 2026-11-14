namespace gAPI.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class OverrideTimeoutInSecondsAttribute : Attribute
{
    public OverrideTimeoutInSecondsAttribute(double timeoutInSeconds)
    {
        TimeoutInSeconds = timeoutInSeconds;
    }

    public double TimeoutInSeconds { get; }
}
