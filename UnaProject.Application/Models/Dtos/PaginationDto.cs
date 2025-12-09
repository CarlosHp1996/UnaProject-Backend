namespace UnaProject.Application.Models.Dtos
{
    public class PaginationDto
    {
        public int? CurrentPage { get; set; }
        public int? PageSize { get; set; }
        public int? TotalItems { get; set; }
        public int? TotalPages { get; set; }
    }
}
