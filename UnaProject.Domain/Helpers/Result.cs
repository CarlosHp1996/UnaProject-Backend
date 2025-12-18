using System.Net;

namespace UnaProject.Domain.Helpers
{
    public class Result<T>
    {
        public T? Value { get; set; }
        public int Count { get; set; }
        public bool HasSuccess { get; set; }
        public bool HasError => !HasSuccess;
        public string? Message { get; set; }
        public IList<string> Errors { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }
        public DateTime DataRequisicao { get; set; }
        public string? ErrorMessage => Errors?.FirstOrDefault();

        public Result()
        {
            HasSuccess = true;
            HttpStatusCode = HttpStatusCode.OK;
            Errors = new List<string>();
            DataRequisicao = DateTime.Now;
        }

        public Result(T value)
        {
            Value = value;
            HasSuccess = true;
            HttpStatusCode = HttpStatusCode.OK;
            Errors = new List<string>();
            DataRequisicao = DateTime.Now;
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>(value);
        }

        public static Result<T> Failure(string errorMessage)
        {
            var result = new Result<T>();
            result.WithError(errorMessage);
            return result;
        }

        public void WithError(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.BadRequest;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }

        public void WithUnauthorized(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.Unauthorized;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }

        public void WithNotFound(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.NotFound;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }

        public void WithException(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.InternalServerError;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }
    }

    public class Result
    {
        public bool HasSuccess { get; set; }
        public bool HasError => !HasSuccess;
        public string? Message { get; set; }
        public IList<string> Errors { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }
        public DateTime DataRequisicao { get; set; }
        public string? ErrorMessage => Errors?.FirstOrDefault();

        public Result()
        {
            HasSuccess = true;
            HttpStatusCode = HttpStatusCode.OK;
            Errors = new List<string>();
            DataRequisicao = DateTime.Now;
        }

        public static Result Success()
        {
            return new Result();
        }

        public static Result Failure(string errorMessage)
        {
            var result = new Result();
            result.WithError(errorMessage);
            return result;
        }

        public void WithError(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.BadRequest;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }

        public void WithUnauthorized(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.Unauthorized;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }

        public void WithNotFound(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.NotFound;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }

        public void WithException(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.InternalServerError;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }
    }

    /// Class for results that need to return value types or reference types.
    public class ResultValue<T>
    {
        public T? Value { get; set; }
        public bool HasSuccess { get; set; }
        public bool HasError => !HasSuccess;
        public string? Message { get; set; }
        public IList<string> Errors { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }
        public DateTime DataRequisicao { get; set; }
        public string? ErrorMessage => Errors?.FirstOrDefault();

        public ResultValue()
        {
            HasSuccess = true;
            HttpStatusCode = HttpStatusCode.OK;
            Errors = new List<string>();
            DataRequisicao = DateTime.Now;
        }

        public ResultValue(T value)
        {
            Value = value;
            HasSuccess = true;
            HttpStatusCode = HttpStatusCode.OK;
            Errors = new List<string>();
            DataRequisicao = DateTime.Now;
        }

        public static ResultValue<T> Success(T value)
        {
            return new ResultValue<T>(value);
        }

        public static ResultValue<T> Failure(string errorMessage)
        {
            var result = new ResultValue<T>();
            result.WithError(errorMessage);
            return result;
        }

        public void WithError(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.BadRequest;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }

        public void WithUnauthorized(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.Unauthorized;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }

        public void WithNotFound(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.NotFound;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }

        public void WithException(string errorMessage)
        {
            HttpStatusCode = HttpStatusCode.InternalServerError;
            HasSuccess = false;
            Errors.Add(errorMessage);
            DataRequisicao = DateTime.Now;
        }
    }
}
