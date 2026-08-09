using Microsoft.AspNetCore.Components;

namespace gAPI.Core.Client;

public class UriNavigationManager(
    NavigationManager navigation)
    : IUriNavigationManager
{
    string IUriNavigationManager.GetPathAndQuery()
        => navigation.ToBaseRelativePath(navigation.Uri);
}
