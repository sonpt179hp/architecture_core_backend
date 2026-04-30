namespace GovDocs.Domain.Primitives;

// Marks an entity as the root of an aggregate boundary.
// Only the aggregate root may be referenced by external aggregates.
// All persistence operations are performed through the aggregate root.
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id)
    {
    }

    // Required by EF Core
    protected AggregateRoot()
    {
    }
}
