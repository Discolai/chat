using Application.Conversations;
using Core.Conversation;
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

var kernelBuilder = builder.Services.AddKernel();
if (builder.Environment.IsDevelopment())
{
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    kernelBuilder.AddOllamaChatCompletion("phi3", endpoint: new Uri("http://localhost:11434"));
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                                 //kernelBuilder.AddOpenAIChatCompletion("openai/o4-mini", new Uri("https://models.inference.ai.azure.com"), builder.Configuration["GH_PAT"]);
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(options => options.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:3000").AllowCredentials());
    app.MapScalarApiReference();
}

app
    .MapDefaultEndpoints()
    .MapConversationEndpoints();

app.MapHub<ConversationHub>("/hubs/conversations");

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.Run();
