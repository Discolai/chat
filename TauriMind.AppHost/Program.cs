var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Application>("application");

builder.Build().Run();
