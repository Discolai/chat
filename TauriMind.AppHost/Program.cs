var builder = DistributedApplication.CreateBuilder(args);

var application = builder.AddProject<Projects.Application>("application")
    .WithUrlForEndpoint("https", url =>
    {
        url.DisplayText = "Scalar (HTTPS)";
        url.Url = "/scalar";
    });

builder.Build().Run();
