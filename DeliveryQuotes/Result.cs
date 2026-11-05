namespace DeliveryQuotes;

public abstract record Result<T>
{
    private Result() { }

    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(string Error) : Result<T>;

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;

    public static Result<T> Ok(T value) => new Success(value);
    public static Result<T> Fail(string error) => new Failure(error);

    public Result<U> Map<U>(Func<T, U> mapper)
    {
        return this switch
        {
            Success s => Result<U>.Ok(mapper(s.Value)),
            Failure f => Result<U>.Fail(f.Error),
            _ => throw new InvalidOperationException()
        };
    }

    public Result<U> Bind<U>(Func<T, Result<U>> binder)
    {
        return this switch
        {
            Success s => binder(s.Value),
            Failure f => Result<U>.Fail(f.Error),
            _ => throw new InvalidOperationException()
        };
    }

    public R Match<R>(Func<T, R> onSuccess, Func<string, R> onFailure)
    {
        return this switch
        {
            Success s => onSuccess(s.Value),
            Failure f => onFailure(f.Error),
            _ => throw new InvalidOperationException()
        };
    }
}
