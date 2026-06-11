using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CaseFraudSys.Api.Infrastructure.Swagger;

public static class SwaggerExtensions
{
    private static readonly (string Name, string Description)[] TagOrder =
    [
        ("Saúde da API", "Verificação de disponibilidade do serviço."),
        ("Gestão de Limites PIX", "Cadastro, consulta, alteração e remoção de limites PIX por conta (requisitos 2.1–2.4)."),
        ("Transações PIX", "Avaliação e processamento de transações PIX com idempotência (requisito 2.5).")
    ];

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CaseFraudSys API",
                Version = "v1",
                Description =
                    "API de gestão de limites PIX do Banco KRT (desafio técnico BTG Pactual). " +
                    "Permite cadastrar, consultar, alterar e remover limites por conta, " +
                    "além de processar transações PIX com débito atômico e idempotência via transactionId."
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            options.TagActionsBy(api =>
            {
                if (api.ActionDescriptor is ControllerActionDescriptor descriptor)
                {
                    var tags = descriptor.EndpointMetadata
                        .OfType<TagsAttribute>()
                        .SelectMany(t => t.Tags)
                        .Distinct()
                        .ToArray();

                    if (tags.Length > 0)
                        return tags;
                }

                return [api.GroupName ?? "Outros"];
            });

            options.DocumentFilter<TagOrderDocumentFilter>();
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "CaseFraudSys — Documentação da API";
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CaseFraudSys v1");
        });

        return app;
    }

    private sealed class TagOrderDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var orderedTags = new List<OpenApiTag>();

            foreach (var (name, description) in TagOrder)
            {
                if (swaggerDoc.Tags?.FirstOrDefault(t => t.Name == name) is { } existing)
                    orderedTags.Add(new OpenApiTag { Name = existing.Name, Description = description });
                else
                    orderedTags.Add(new OpenApiTag { Name = name, Description = description });
            }

            if (swaggerDoc.Tags is not null)
            {
                foreach (var tag in swaggerDoc.Tags)
                {
                    if (orderedTags.All(t => t.Name != tag.Name))
                        orderedTags.Add(tag);
                }
            }

            swaggerDoc.Tags = new HashSet<OpenApiTag>(orderedTags);
        }
    }
}
