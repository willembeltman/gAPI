using gAPI.Generated;
using gAPI.Test.MicroService2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;

var invariantCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = invariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = invariantCulture;

var builder = Host.CreateApplicationBuilder(args);

#if DEBUG
if (File.Exists("appsettings.Development.json"))
{
    using var stream = File.OpenRead("appsettings.Development.json");
    if (stream != null)
        builder.Configuration.AddJsonStream(stream);
}
#endif

builder.Services.AddAutoBus(builder.Configuration);
builder.Services.AddHostedService<Service1>();

var app = builder.Build();
app.Run();