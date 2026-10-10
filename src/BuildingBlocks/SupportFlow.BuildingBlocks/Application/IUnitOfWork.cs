namespace SupportFlow.BuildingBlocks.Application;

public interface IUnitOfWork
{
    /// <summary>
    /// Runs the operation in one database transaction and commits the changes made through repositories.
    /// Any exception rolls the whole transaction back and is rethrown.
    /// </summary>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
