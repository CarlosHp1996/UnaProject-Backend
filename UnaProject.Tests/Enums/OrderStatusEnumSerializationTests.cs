using System.Text.Json;
using FluentAssertions;
using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Enums;
using Xunit;

namespace UnaProject.Tests.Enums
{
    /// <summary>
    /// Garante o contrato de wire com o frontend: EnumOrderStatus / EnumPaymentStatus
    /// serializam em MINÚSCULAS no JSON (pending, paid, ...) e desserializam case-insensitive,
    /// via LowerCaseEnumConverter aplicado por-enum. Se algum membro mudar de nome/ordem,
    /// estes testes quebram antes do frontend.
    /// </summary>
    public class OrderStatusEnumSerializationTests
    {
        [Theory]
        [InlineData(EnumOrderStatus.Pending, "pending")]
        [InlineData(EnumOrderStatus.Processing, "processing")]
        [InlineData(EnumOrderStatus.Shipped, "shipped")]
        [InlineData(EnumOrderStatus.Delivered, "delivered")]
        [InlineData(EnumOrderStatus.Cancelled, "cancelled")]
        public void EnumOrderStatus_Serializes_Lowercase(EnumOrderStatus status, string expected)
        {
            var json = JsonSerializer.Serialize(status);

            json.Should().Be($"\"{expected}\"");
        }

        [Theory]
        [InlineData(EnumPaymentStatus.Pending, "pending")]
        [InlineData(EnumPaymentStatus.Approved, "approved")]
        [InlineData(EnumPaymentStatus.Paid, "paid")]
        [InlineData(EnumPaymentStatus.Failed, "failed")]
        public void EnumPaymentStatus_Serializes_Lowercase(EnumPaymentStatus status, string expected)
        {
            var json = JsonSerializer.Serialize(status);

            json.Should().Be($"\"{expected}\"");
        }

        [Theory]
        [InlineData("\"paid\"", EnumPaymentStatus.Paid)]
        [InlineData("\"PAID\"", EnumPaymentStatus.Paid)]
        [InlineData("\"Pending\"", EnumPaymentStatus.Pending)]
        public void EnumPaymentStatus_Deserializes_CaseInsensitive(string json, EnumPaymentStatus expected)
        {
            var result = JsonSerializer.Deserialize<EnumPaymentStatus>(json);

            result.Should().Be(expected);
        }

        [Fact]
        public void OrderDto_Emits_Lowercase_Status_Fields()
        {
            var dto = new OrderDto
            {
                Status = EnumOrderStatus.Shipped,
                PaymentStatus = EnumPaymentStatus.Paid
            };

            var json = JsonSerializer.Serialize(dto);

            json.Should().Contain("\"Status\":\"shipped\"");
            json.Should().Contain("\"PaymentStatus\":\"paid\"");
        }
    }
}
