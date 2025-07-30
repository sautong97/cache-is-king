using CacheIsKing.Aggregation;
using CacheIsKing.Caching.Extensions;
using CacheIsKing.Core.Interfaces;
using CacheIsKing.Providers.TomTom;
using CacheIsKing.Providers.Here;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure caching
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
builder.Services.AddHybridCaching(redisConnectionString);

// Configure HTTP clients
builder.Services.AddHttpClient<TomTomLocationService>();
builder.Services.AddHttpClient<HereLocationService>();

// Register location providers
var tomtomApiKey = builder.Configuration["ApiKeys:TomTom"] ?? "your-tomtom-api-key";
var hereApiKey = builder.Configuration["ApiKeys:HERE"] ?? "your-here-api-key";

builder.Services.AddTransient<ILocationProviderService>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(TomTomLocationService));
    var logger = provider.GetRequiredService<ILogger<TomTomLocationService>>();
    return new TomTomLocationService(httpClient, logger, tomtomApiKey);
});

builder.Services.AddTransient<ILocationProviderService>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(HereLocationService));
    var logger = provider.GetRequiredService<ILogger<HereLocationService>>();
    return new HereLocationService(httpClient, logger, hereApiKey);
});

// Register aggregation service
builder.Services.AddScoped<ILocationService, LocationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
