using gAPI.Core.Client.Interfaces;

namespace gAPI.Core.Client.Navigation;

public class StaticNavigationManager : IUriNavigationManager
{
    public string GetPathAndQuery()
    {
        return "~";
    }
}
