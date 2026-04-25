namespace PracticeX.Application.Common;

public readonly record struct OperationError(string Code, string Message);

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, OperationError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public OperationError? Error { get; }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(string code, string message) => new(false, default, new OperationError(code, message));
}
