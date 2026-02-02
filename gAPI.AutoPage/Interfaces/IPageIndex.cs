
using System.Collections.Generic;

namespace gAPI.AutoPage.Interfaces
{
    public interface IPageIndex 
    {
        string Route { get; }
        string? Title { get; }
        IEnumerable<IPage> Pages { get; }
    }
}