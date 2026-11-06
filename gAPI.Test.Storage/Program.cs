using gAPI.Generated;
using gAPI.Storage.Server.Extensions;
using System.Globalization;

var invariantCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = invariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = invariantCulture;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoBus(builder.Configuration);
builder.Services.AddStorageServer(builder.Configuration);

var app = builder.Build();
app.MapStorageServer();
app.Run();