using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Interfejs Unit of Work dla zarządzania transakcjami
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Rozpoczyna transakcję
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Zatwierdza transakcję
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cofa transakcję
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Wykonuje operację w transakcji
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<MySqlTransaction, Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wykonuje operację w transakcji (bez zwracanej wartości)
    /// </summary>
    Task ExecuteInTransactionAsync(Func<MySqlTransaction, Task> operation, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementacja Unit of Work dla MySQL
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly DatabaseContext _context;
    private MySqlTransaction? _transaction;
    private bool _disposed = false;

    public UnitOfWork(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
            throw new InvalidOperationException("Transakcja już została rozpoczęta.");

        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        _transaction = await connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            return;

        var t = _transaction;
        _transaction = null;
        try
        {
            await t.CommitAsync(cancellationToken);
            if (t.Connection != null)
                await t.Connection.CloseAsync();
        }
        finally
        {
            t.Dispose();
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            return;

        var t = _transaction;
        _transaction = null;
        try
        {
            await t.RollbackAsync(cancellationToken);
            if (t.Connection != null)
                await t.Connection.CloseAsync();
        }
        finally
        {
            t.Dispose();
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<MySqlTransaction, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            // Zagnieżdżenie: tylko wykonaj operację, bez Begin/Commit/Rollback
            return await operation(_transaction);
        }

        // Root-transakcja: Begin → operation → Commit lub Rollback, sprzątanie tylko raz
        await BeginTransactionAsync(cancellationToken);
        var committed = false;
        try
        {
            var result = await operation(_transaction!);
            await CommitAsync(cancellationToken);
            committed = true;
            return result;
        }
        catch
        {
            if (!committed && _transaction != null)
                await RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                try { _transaction.Dispose(); } catch { }
                _transaction = null;
            }
        }
    }

    public async Task ExecuteInTransactionAsync(Func<MySqlTransaction, Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async transaction =>
        {
            await operation(transaction);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            _disposed = true;
        }
    }
}
