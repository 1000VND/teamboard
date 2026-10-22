namespace TeamBoard.Application.Common;

public enum ErrorType
{
    NotFound,
    Validation
}

public sealed record Error(ErrorType Type, string Message)
{
    public static Error NotFound(string message) => new(ErrorType.NotFound, message);
    public static Error Validation(string message) => new(ErrorType.Validation, message);
}

// Outcome of a command: success, or an Error the API turns into 404 / 400.
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);
    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private Result(T? value, Error? error) : base(error) => Value = value;

    // Only meaningful when IsSuccess is true.
    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, null);
    public static new Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
