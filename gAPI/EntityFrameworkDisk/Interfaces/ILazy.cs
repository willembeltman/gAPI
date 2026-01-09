namespace gAPI.EntityFrameworkDisk;


/// <summary>
/// Represents a lazily evaluated value container.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public interface ILazy<T>
{
    /// <summary>
    /// Gets or sets the lazily loaded value.
    /// </summary>
    T? Value { get; set; }
}