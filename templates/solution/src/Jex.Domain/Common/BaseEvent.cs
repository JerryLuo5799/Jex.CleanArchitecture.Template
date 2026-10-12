namespace Jex.Domain.Common;

/// <summary>
/// Base class for domain events raised by entities. Handlers (<c>INotificationHandler&lt;TEvent&gt;</c>
/// in Jex.Application) run inside the command's transaction, so they may only do database work;
/// external side effects (email, HTTP) belong in an outbox (see ADR-0005).
/// </summary>
public abstract class BaseEvent : INotification;
