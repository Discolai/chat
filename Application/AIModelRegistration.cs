using Domain;
using Microsoft.SemanticKernel;

namespace Application;
public static class AIModelRegistration
{
    private const string _configuredAIModelsSectionName = "AI";

    public static WebApplicationBuilder AddAIModels(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection(_configuredAIModelsSectionName);
        var modelConfiguration = section.Get<AIModelConfiguration>();
        if (modelConfiguration is null || !modelConfiguration.Models.Any())
        {
            throw new Exception("No AI models registered");
        }
        if (modelConfiguration.Models.DistinctBy(x => x.Name).Count() != modelConfiguration.Models.Length)
        {
            throw new Exception("All AI models must be unique");
        }

        builder.Services.Configure<AIModelConfiguration>(section);

        var kernelBuilder = builder.Services.AddKernel();
        foreach (var model in modelConfiguration.Models)
        {
            switch (model.Provider)
            {
                case AIModelProvider.Ollama:
                    ArgumentException.ThrowIfNullOrWhiteSpace(model.Endpoint);
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    kernelBuilder.AddOllamaChatCompletion(model.Name, endpoint: new Uri(model.Endpoint), serviceId: model.Name);
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    break;
                case AIModelProvider.OpenAi:
                    ArgumentException.ThrowIfNullOrWhiteSpace(model.ApiKey);
                    if (string.IsNullOrWhiteSpace(model.Endpoint))
                    {
                        kernelBuilder.AddOpenAIChatCompletion(model.Name, model.ApiKey, serviceId: model.Name);
                    }
                    else
                    {
                        kernelBuilder.AddOpenAIChatCompletion(model.Name, new Uri(model.Endpoint), model.ApiKey, serviceId: model.Name);
                    }
                    break;
                default:
                    throw new NotSupportedException($"{model.Provider} is not supported!");

            }
        }
        return builder;
    }
}
