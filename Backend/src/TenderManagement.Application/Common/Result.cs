namespace TenderManagement.Application.Common
{
    /// <summary>Generic operation result wrapper so services never need to throw for expected failure cases.</summary>
    public class Result<T>
    {
        public bool Success { get; private set; }
        public string? Message { get; private set; }
        public T? Data { get; private set; }

        public static Result<T> Ok(T data, string? message = null) => new() { Success = true, Data = data, Message = message };
        public static Result<T> Fail(string message) => new() { Success = false, Message = message };
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
