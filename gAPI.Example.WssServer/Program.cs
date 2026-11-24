using gAPI.Core.Server.Extensions;
using gAPI.Generated;
using System.Globalization;

var invariantCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = invariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = invariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoPlane(builder.Configuration);
builder.Services.AddAutoWssServer(builder.Configuration);
builder.Services.AddAutoAuthServer(builder.Configuration);

var app = builder.Build();
app.MapAutoWssServer();
app.MapAutoAuthServer();
app.UseHttpsRedirection();
app.Run();