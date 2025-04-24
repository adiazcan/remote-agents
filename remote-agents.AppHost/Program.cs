using Microsoft.Extensions.Configuration;
using RemoteAgents.AppHost;
using System.Diagnostics;
using YamlDotNet.Core.Tokens;

var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());

builder.Configuration.AddJsonFile($@"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                     .AddJsonFile($@"appsettings.{Environment.UserName}.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();

var flightService = builder.AddProject<Projects.remote_agents_FlightService>("flightservice");

var apiService = builder.AddProject<Projects.remote_agents_ApiService>("apiservice")
    .WithReference(flightService)
    .WithHttpsHealthCheck("/health");

builder.AddProject<Projects.remote_agents_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpsHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
