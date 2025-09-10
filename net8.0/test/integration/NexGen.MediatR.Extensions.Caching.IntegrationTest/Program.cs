using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts;

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
    // opt.UseRedisCache(builder.Configuration.GetConnectionString("Redis"));
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
