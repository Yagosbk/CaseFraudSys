using System.Net;
using System.Text.Json;
using CaseFraudSys.Api.Domain.Exceptions;
using CaseFraudSys.Api.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace CaseFraudSys.Api.Tests.Infrastructure;

[Trait("Category", "Unit")]
public class ErrorHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenApiException_ReturnsExpectedStatusAndMessage()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new ApiException("Conta não encontrada.", HttpStatusCode.NotFound),
            NullLogger<ErrorHandlingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.False(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Conta não encontrada.", document.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_Returns500()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new InvalidOperationException("falha inesperada"),
            NullLogger<ErrorHandlingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("Erro interno do servidor.", document.RootElement.GetProperty("message").GetString());
    }
}
