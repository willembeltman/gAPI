using gAPI.Interfaces;
using gAPI.Test.Api;
using gAPI.Test.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Authentication services
builder.Services.AddScoped<ServerAuthenticationService>();
builder.Services.AddScoped<gAPI.Interfaces.IServerAuthenticationService>(sp => sp.GetRequiredService<ServerAuthenticationService>());
builder.Services.AddScoped<IServerAuthenticationService>(sp => sp.GetRequiredService<ServerAuthenticationService>());

// Http context accessor for gAPI
builder.Services.AddHttpContextAccessor();

// Remaining services
builder.Services.AddAutoApiServices();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();