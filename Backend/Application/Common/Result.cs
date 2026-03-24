namespace WhatsAppParser.Application.Common;

public sealed class Result<T>
{
    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(string error) { IsSuccess = false; Error = error; }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}

public sealed class Result
{
    private Result() { IsSuccess = true; }
    private Result(string error) { IsSuccess = false; Error = error; }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    public static Result Success() => new();
    public static Result Failure(string error) => new(error);
}
