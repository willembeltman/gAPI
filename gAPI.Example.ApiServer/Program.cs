using gAPI.Example.ApiServer.Server;
using gAPI.Generated;
using System.Globalization;

var invariantCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = invariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = invariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoBus(builder.Configuration);
builder.Services.AddAutoApiServer(builder.Configuration);
builder.Services.AddAutoAuthServer(builder.Configuration);
builder.Services.AddSingleton<Cache>();

var app = builder.Build();
app.MapAutoApiServer();
app.MapAutoAuthServer();
app.UseHttpsRedirection();
app.Run();