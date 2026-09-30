namespace OverLoad.Repositories.Interfaces;

/// <summary>
/// Commits every change tracked by the repositories of the current request in one database
/// transaction. Use it with the repositories' Add methods when several writes must succeed or
/// fail together (payments, wallet debits).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync();

    /// <summary>Forgets tracked entities so a retry re-reads fresh values from the database.</summary>
    void ClearChanges();
}
