// Third-party namespaces live here so that source files only import System.* and project
// namespaces. That keeps `using` ordering independent of the project name (see CONTRIBUTING.md).
global using FluentValidation;
global using FluentValidation.Results;
global using MediatR;
global using MediatR.Pipeline;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

// The application ValidationException (code 4000) wins over FluentValidation.ValidationException.
global using ValidationException = Jex.Application.Common.Exceptions.ValidationException;
