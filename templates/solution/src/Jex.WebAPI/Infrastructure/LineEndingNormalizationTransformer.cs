using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Jex.WebAPI.Infrastructure;

/// <summary>
/// Normalizes CRLF to LF in descriptions taken from XML doc comments, which are emitted with the
/// platform newline. Keeps the committed openapi.json identical on Windows and Linux.
/// </summary>
public sealed class LineEndingNormalizationTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var path in document.Paths?.Values ?? Enumerable.Empty<IOpenApiPathItem>())
        {
            foreach (var operation in path.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
            {
                Normalize(operation);
            }
        }

        foreach (var schema in document.Components?.Schemas?.Values ?? Enumerable.Empty<IOpenApiSchema>())
        {
            Normalize(schema, []);
        }

        return Task.CompletedTask;
    }

    private static void Normalize(OpenApiOperation operation)
    {
        operation.Summary = Lf(operation.Summary);
        operation.Description = Lf(operation.Description);

        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter is OpenApiParameter concrete)
            {
                concrete.Description = Lf(concrete.Description);
            }
        }

        if (operation.RequestBody is OpenApiRequestBody body)
        {
            body.Description = Lf(body.Description);
        }

        foreach (var response in operation.Responses?.Values ?? Enumerable.Empty<IOpenApiResponse>())
        {
            if (response is OpenApiResponse concrete)
            {
                concrete.Description = Lf(concrete.Description);
            }
        }
    }

    private static void Normalize(IOpenApiSchema schema, HashSet<IOpenApiSchema> visited)
    {
        if (schema is not OpenApiSchema concrete || !visited.Add(concrete))
        {
            return;
        }

        concrete.Description = Lf(concrete.Description);

        foreach (var property in concrete.Properties?.Values ?? Enumerable.Empty<IOpenApiSchema>())
        {
            Normalize(property, visited);
        }

        if (concrete.Items is not null)
        {
            Normalize(concrete.Items, visited);
        }

        foreach (var child in (concrete.AllOf ?? []).Concat(concrete.AnyOf ?? []).Concat(concrete.OneOf ?? []))
        {
            Normalize(child, visited);
        }
    }

    private static string? Lf(string? value) => value?.Replace("\r\n", "\n", StringComparison.Ordinal);
}
