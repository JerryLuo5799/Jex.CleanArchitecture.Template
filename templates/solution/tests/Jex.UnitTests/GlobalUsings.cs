global using FluentValidation;
global using FluentValidation.TestHelper;
global using MediatR;
global using Microsoft.Extensions.Logging;
global using Moq;
global using Shouldly;
global using Xunit;

// The application ValidationException (code 4000) wins over FluentValidation.ValidationException.
global using ValidationException = Jex.Application.Common.Exceptions.ValidationException;
