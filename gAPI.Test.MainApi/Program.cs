using gAPI.Test.Main.Server;
using System.Globalization;
using gAPI.Generated;

var invariantCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = invariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = invariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoBus(builder.Configuration);
builder.Services.AddAutoWssServer(builder.Configuration);
builder.Services.AddAutoAuthServer(builder.Configuration);
builder.Services.AddHostedService<MainService>();


var app = builder.Build();
app.MapAutoWssServer();
app.MapAutoAuthServer();
app.UseHttpsRedirection();
app.Run();