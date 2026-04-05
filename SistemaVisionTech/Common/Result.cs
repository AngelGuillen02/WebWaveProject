namespace SistemaVisionTech.Common
{
    public class Result<T>
    {
        public bool Success { get; private set; }
        public string? Error { get; private set; }
        public T? Data { get; private set; }

        public bool IsValidationError { get; private set; }

        private Result() { }

        public static Result<T> Ok(T data) =>
            new()
            { Success = true, Data = data };

        public static Result<T> Fail(string error, bool isValidation = false) =>
            new()
            {
                Success = false,
                Error = error,
                IsValidationError = isValidation
            };
    }

    public class Result
    {
        public bool Success { get; private set; }
        public string? Error { get; private set; }
        public bool IsValidationError { get; private set; }

        private Result() { }

        public static Result Ok() => new() { Success = true };

        public static Result Fail(string error, bool isValidation = false) =>
            new()
            {
                Success = false,
                Error = error,
                IsValidationError = isValidation
            };
    }
}