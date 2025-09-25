using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Context;
using NexGen.MediatR.Extensions.EntityFramework.Configurations;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MediatR Services
builder.Services.AddMediatR(opt =>
    opt.RegisterServicesFromAssembly(Assembly.GetEntryAssembly()!));

builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseMemoryCache();
    // opt.UseRedisCache(builder.Configuration.GetConnectionString("Redis")!);
    // opt.UseGarnetCache(builder.Configuration.GetConnectionString("Garnet")!);

    opt.UseEntityFrameworkAutoEvict<AppDbContext>(contextOptionsBuilder =>
    {
        var sqlServerConnectionString = builder.Configuration.GetConnectionString("SqlServer");
        contextOptionsBuilder.UseSqlServer(sqlServerConnectionString);
    });
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