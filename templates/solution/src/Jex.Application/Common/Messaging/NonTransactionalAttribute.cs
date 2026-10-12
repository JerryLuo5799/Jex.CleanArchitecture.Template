namespace Jex.Application.Common.Messaging;

/// <summary>
/// Opts a command out of the unit of work. The behaviour will not open a transaction for it;
/// if an outer transaction already exists the command simply joins it (no independent commit).
/// Use for commands whose writes must persist even when the command "fails", e.g. login attempts
/// that increment the lockout counter and then throw.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class NonTransactionalAttribute : Attribute;
