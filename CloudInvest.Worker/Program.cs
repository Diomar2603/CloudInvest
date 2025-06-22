using Amazon.SQS;
using CloudInvest.Infrastructure.Data;
using CloudInvest.Worker;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Polly;
using Polly.Registry;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddSingleton<IAmazonSQS, AmazonSQSClient>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();