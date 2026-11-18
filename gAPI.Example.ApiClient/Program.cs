using gAPI.Generated;
using Microsoft.Extensions.Hosting;
using System.Globalization;

var invariantCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = invariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = invariantCulture;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAutoApiClient(builder.Configuration);
builder.Services.AddAutoAuthClient(builder.Configuration);

var app = builder.Build();
app.Run();