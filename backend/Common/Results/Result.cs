using System;

namespace SuperMegaUnboxDeluxe.Common.Results;

public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value) => _value = value;
    private Result(Error error) => Error = error;

    public bool IsSuccess => Error is null;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");
    public Error? Error { get; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
}
