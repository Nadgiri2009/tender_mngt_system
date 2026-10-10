using System.Linq.Expressions;

namespace TenderManagement.Application.Interfaces
{
    /// <summary>Generic repository abstraction implemented by Infrastructure/Repositories.</summary>
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(object id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
        Task<int> SaveChangesAsync();
    }
}
