using Jex.Application;
using Jex.Application.Common.Exceptions;
using Jex.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ── Layer registrations ─────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── ASP.NET Core services ───────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── ProblemDetails for RFC 7807 error responses ─────────────────────────────
builder.Services.AddProblemDetails();

var app = builder.Build();

// ── Global exception handler ────────────────────────────────────────────────
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
            _ => (StatusCodes.Status500InternalServerError, "Server Error")
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception?.Message
        };

        if (exception is ValidationException ve)
            problem.Extensions["errors"] = ve.Errors;

        await context.Response.WriteAsJsonAsync(problem);
    });
});

// ── OpenAPI / Swagger ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
