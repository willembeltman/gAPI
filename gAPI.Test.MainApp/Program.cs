using gAPI.Generated;
using gAPI.Test.Main.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;

var invariantCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = invariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = invariantCulture;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAutoWssClient(builder.Configuration);
builder.Services.AddAutoAuthClient(builder.Configuration);

builder.Services.AddHostedService<MainHub>();

var app = builder.Build();
app.Run();