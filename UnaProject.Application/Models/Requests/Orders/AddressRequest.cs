namespace UnaProject.Application.Models.Requests.Orders
{
    public class AddressRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string? Complement { get; set; }
        public string Neighborhood { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
