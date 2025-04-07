using MicroM.Web.Services;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

#if DEBUG
IdentityModelEventSource.ShowPII = true;
#endif

builder.Services.AddCors(options => options.AddDefaultPolicy(
    builder =>
    {
        builder.WithOrigins(allowedOrigins ?? ["https://noone"]).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    })
);

// Add MicroM services.
builder.Services.AddMicroMApiServices(builder.Configuration);

var app = builder.Build();

app.UseMicroMWebAPI();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseCors();

// Uncomment the following line to enable debugging routes
//app.UseDebugRoutes();

app.MapGet("/", () => "MicroM API");

app.MapControllers();

app.Run();

