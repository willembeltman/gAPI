using gAPI.Generated;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Globalization;

var invariantCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = invariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = invariantCulture;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appconfig.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddAutoApiClient(builder.Configuration);
builder.Services.AddAutoAuthClient(builder.Configuration);

var app = builder.Build();
app.Run();