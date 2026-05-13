
using System.Collections.Generic;

namespace gAPI.AutoComponent.Interfaces
{
    public interface IPageIndex
    {
        string Route { get; }
        string? Title { get; }
        IEnumerable<IPage> Pages { get; }
    }
}