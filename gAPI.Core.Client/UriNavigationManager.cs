using Microsoft.AspNetCore.Components;

namespace gAPI.Core.Client;

public class UriNavigationManager(
    NavigationManager navigation)
    : INavigationManager
{
    string INavigationManager.GetPathAndQuery()
        => navigation.ToBaseRelativePath(navigation.Uri);
}
