namespace UnaProject.Application.Models.Filters
{
    public class GetUsersRequestFilter : BaseRequestFilter
    {
        public string? SearchTerm { get; set; }
    }
}
