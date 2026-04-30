namespace GovDocs.Domain.Primitives;

// Marker interface for domain events.
// Domain layer has zero external NuGet dependencies.
// Infrastructure converts IDomainEvent to INotification before publishing via Mediator.
public interface IDomainEvent
{
}
