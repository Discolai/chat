using Application.Conversations;
using Core.Conversation;
using Microsoft.SemanticKernel;
using Scalar.AspNetCore;
using TauriMind.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddCors();

builder.UseOrleans(silo =>
{
    if (builder.Environment.IsDevelopment())
    {
        silo.AddMemoryGrainStorageAsDefault();
        silo.UseLocalhostClustering();
    }
});

var kernelBuilder = builder.Services.AddKernel();
if (builder.Environment.IsDevelopment())
{
    kernelBuilder.AddOpenAIChatCompletion("DeepSeek-R1-0528", new Uri("https://models.inference.ai.azure.com"), builder.Configuration["GH_PAT"]);
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(options => options.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
    app.MapScalarApiReference();
}

app
    .MapDefaultEndpoints()
    .MapConversationEndpoints();

app.MapHub<ConversationHub>("/hubs/conversations");

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.Run();
