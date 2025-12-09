namespace UnaProject.Application.Models.Dtos
{
    public class FiltersDto
    {
        public List<FilterQuantityRangeDto>? QuantityRanges { get; set; } = new List<FilterQuantityRangeDto>();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }

    public class FilterAttributeDto
    {
        public string? Value { get; set; }
        public int? ProductCount { get; set; }
    }

    public class FilterQuantityRangeDto
    {
        public int? MinQuantity { get; set; }
        public int? MaxQuantity { get; set; }
        public int? ProductCount { get; set; }
    }
}
