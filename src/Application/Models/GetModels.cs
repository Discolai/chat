
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Application.Models;

public static class GetModels
{
    public static Ok<IEnumerable<AIModel>> Handle(IOptions<AIModelConfiguration> aIModelConfiguration)
    {
        return TypedResults.Ok(aIModelConfiguration.Value.Models.Select(x => x.ToAIModel()));
    }
}
