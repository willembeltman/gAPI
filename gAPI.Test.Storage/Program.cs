using gAPI.Storage.Server;
using System.Globalization;

var invariantCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = invariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = invariantCulture;

var builder = WebApplication.CreateBuilder(args);
builder.AddStorageServer();

var app = builder.Build();
app.MapStorageServer();
app.Run();