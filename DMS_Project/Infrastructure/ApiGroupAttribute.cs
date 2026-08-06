namespace DMS_Project.Infrastructure
{
    /// <summary>
    /// Đánh dấu controller/action chỉ được phục vụ trên một nhóm API cụ thể.
    /// Kết hợp với middleware filter port để tách URL theo cổng mà không cần tách process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ApiGroupAttribute : Attribute
    {
        public string Name { get; }

        public ApiGroupAttribute(string name)
        {
            Name = name;
        }
    }
}