using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Garnet.Extensions;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts.GetWeatherForecasts;
using NexGen.MediatR.Extensions.Caching.Redis.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MediatR Services
builder.Services.AddMediatR(opt => opt.RegisterServicesFromAssembly(typeof(WeatherForecastRequest).Assembly));
builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseMemoryCache();
    //opt.UseRedisCache(builder.Configuration.GetConnectionString("Redis")!);
    //opt.UseGarnetCache(builder.Configuration.GetConnectionString("Garnet")!);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
