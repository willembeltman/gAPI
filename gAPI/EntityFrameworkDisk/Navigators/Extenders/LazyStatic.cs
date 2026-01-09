namespace gAPI.EntityFrameworkDisk.Navigators.Extenders;


/// <summary>
/// Represents a simple lazy container that holds a static entity value. Can be used for linking 
/// foreign navigation properties before adding them to the DbSet.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public class LazyStatic<T> : ILazy<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LazyStatic{T}"/> class 
    /// </summary>
    public LazyStatic()
    {
    }

    /// <summary>
    /// Gets or sets the static entity value.
    /// </summary>
    public T? Value { get; set; } = default;
}