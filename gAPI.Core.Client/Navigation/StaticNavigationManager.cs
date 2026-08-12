using System;
using System.Collections.Generic;
using System.Text;

namespace gAPI.Core.Client.Navigation;

public class StaticNavigationManager : IUriNavigationManager
{
    public string GetPathAndQuery()
    {
        return "~";
    }
}
