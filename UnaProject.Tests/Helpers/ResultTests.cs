using System.Net;
using FluentAssertions;
using UnaProject.Domain.Helpers;
using Xunit;

namespace UnaProject.Tests.Helpers
{
    /// <summary>
    /// Testa o comportamento REAL de Result / Result&lt;T&gt; / ResultValue&lt;T&gt;
    /// (Domain/Helpers/Result.cs). Estas classes sustentam o padrão de retorno
    /// dos handlers CQRS, então o contrato precisa estar coberto.
    /// </summary>
    public class ResultTests
    {
        // ---------- Result<T> ----------

        [Fact]
        public void ResultT_Success_SetsHasSuccessAndValue()
        {
            var result = Result<string>.Success("ok");

            result.HasSuccess.Should().BeTrue();
            result.HasError.Should().BeFalse();
            result.Value.Should().Be("ok");
            result.HttpStatusCode.Should().Be(HttpStatusCode.OK);
            result.Errors.Should().BeEmpty();
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void ResultT_DefaultCtor_IsSuccessByDefault()
        {
            var result = new Result<int>();

            result.HasSuccess.Should().BeTrue();
            result.HttpStatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public void ResultT_WithError_SetsFailureAndBadRequest()
        {
            var result = new Result<string>();

            result.WithError("boom");

            result.HasSuccess.Should().BeFalse();
            result.HasError.Should().BeTrue();
            result.ErrorMessage.Should().Be("boom");
            result.Errors.Should().ContainSingle().Which.Should().Be("boom");
            result.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public void ResultT_Failure_SetsFailureWithMessage()
        {
            var result = Result<string>.Failure("nope");

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("nope");
            result.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public void ResultT_WithException_SetsInternalServerError()
        {
            var result = new Result<string>();

            result.WithException("kaboom");

            result.HasSuccess.Should().BeFalse();
            result.HttpStatusCode.Should().Be(HttpStatusCode.InternalServerError);
            result.ErrorMessage.Should().Be("kaboom");
        }

        [Fact]
        public void ResultT_WithNotFound_And_WithUnauthorized_SetCorrectCodes()
        {
            var notFound = new Result<string>();
            notFound.WithNotFound("missing");
            notFound.HttpStatusCode.Should().Be(HttpStatusCode.NotFound);
            notFound.HasSuccess.Should().BeFalse();

            var unauthorized = new Result<string>();
            unauthorized.WithUnauthorized("no");
            unauthorized.HttpStatusCode.Should().Be(HttpStatusCode.Unauthorized);
            unauthorized.HasSuccess.Should().BeFalse();
        }

        // ---------- Result (non-generic) ----------

        [Fact]
        public void Result_Success_IsSuccess()
        {
            var result = Result.Success();

            result.HasSuccess.Should().BeTrue();
            result.HttpStatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public void Result_Failure_IsError()
        {
            var result = Result.Failure("bad");

            result.HasSuccess.Should().BeFalse();
            result.HasError.Should().BeTrue();
            result.ErrorMessage.Should().Be("bad");
            result.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ---------- ResultValue<T> ----------

        [Fact]
        public void ResultValue_Success_SetsValue()
        {
            var result = ResultValue<bool>.Success(true);

            result.HasSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
            result.HttpStatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public void ResultValue_WithError_SetsFailure()
        {
            var result = new ResultValue<bool>();

            result.WithError("fail");

            result.HasSuccess.Should().BeFalse();
            result.HasError.Should().BeTrue();
            result.ErrorMessage.Should().Be("fail");
            result.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public void ResultValue_Failure_Factory_SetsFailure()
        {
            var result = ResultValue<bool>.Failure("x");

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("x");
        }
    }
}
