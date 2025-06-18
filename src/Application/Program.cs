using Application;
using Application.Conversations;
using Application.Models;
using Core.Conversation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Orleans.Configuration;
using Scalar.AspNetCore;
using ServiceDefaults;
using StackExchange.Redis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

var isGeneratingOpenApiDocument = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

IServiceProvider? serviceProvider = null;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt => opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var signalRBuilder = builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
if (!isGeneratingOpenApiDocument)
{
    signalRBuilder.AddStackExchangeRedis(builder.Configuration.GetConnectionString("redis")!);
}
builder.Services.AddCors();
builder.Services.AddResponseCaching();

if (!isGeneratingOpenApiDocument)
{
    builder.AddServiceDefaults();
    builder.AddRedisClient(connectionName: "redis");
}

builder.UseOrleans(silo =>
{
    if (isGeneratingOpenApiDocument)
    {
        silo.AddMemoryGrainStorageAsDefault();
        silo.UseLocalhostClustering();
    }
    else
    {
        silo.Configure<ClusterOptions>(opts =>
        {
            opts.ClusterId = System.Environment.MachineName;
            opts.ServiceId = "chat.discolai.dev";
        });

        var createMultiplexer = () => Task.FromResult(serviceProvider?.GetRequiredService<IConnectionMultiplexer>()!);
        silo.AddRedisGrainStorageAsDefault(options =>
        {
            options.ConfigurationOptions = new();
            options.CreateMultiplexer = _ => createMultiplexer();
        });
        silo.UseRedisClustering(options =>
        {
            options.ConfigurationOptions = new();
            options.CreateMultiplexer = _ => createMultiplexer();
        });
        silo.UseRedisGrainDirectoryAsDefault(options =>
        {
            options.ConfigurationOptions = new();
            options.CreateMultiplexer = _ => createMultiplexer();
        });
        silo.ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000);
    }
});

if (!isGeneratingOpenApiDocument)
{
    builder.AddAIModels();
}

var jwtOptions = builder.Configuration.Get<JwtOptions>() ?? throw new InvalidOperationException("Invalid jwt configuration");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.MetadataAddress = jwtOptions.MetadataAddress;
    options.Authority = jwtOptions.Authority;
    options.Audience = jwtOptions.Audience;
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for our hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserProvider>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
    app.MapFallbackToFile("/index.html");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(options => options.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:3000").AllowCredentials());
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.UseResponseCaching();

app.MapConversationEndpoints();
app.MapModelsEndpoints();

app.MapHub<ConversationHub>("/hubs/conversations").RequireAuthorization();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

serviceProvider = app.Services;

app.Run();
