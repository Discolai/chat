using Application;
using Application.Conversations;
using Application.Models;
using Core.Conversation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.SemanticKernel;
using Scalar.AspNetCore;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TauriMind.ServiceDefaults;

var isGeneratingOpenApiDocument = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt => opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
builder.Services.AddCors();
builder.Services.AddResponseCaching();

if (!isGeneratingOpenApiDocument)
{
    builder.AddServiceDefaults();
}
builder.UseOrleans(silo =>
{
    if (isGeneratingOpenApiDocument || builder.Environment.IsDevelopment())
    {
        silo.AddMemoryGrainStorageAsDefault();
        silo.UseLocalhostClustering();
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

app.Run();
