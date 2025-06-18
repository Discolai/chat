var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("redis").WithDataVolume(isReadOnly: false).WithLifetime(ContainerLifetime.Persistent).WithRedisInsight();

var application = builder.AddProject<Projects.Application>("application")
    .WithUrlForEndpoint("https", url =>
    {
        url.DisplayText = "Scalar (HTTPS)";
        url.Url = "/scalar";
    })
    .WithReference(cache);

builder.Build().Run();
