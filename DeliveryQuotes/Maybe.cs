namespace DeliveryQuotes;

public abstract record Maybe<T>
{
    private Maybe() { }

    public sealed record Some(T Value) : Maybe<T>;
    public sealed record None : Maybe<T>;

    public bool HasValue => this is Some;

    public static Maybe<T> Just(T value) => new Some(value);
    public static Maybe<T> Empty() => new None();

    public Result<T> ToResult(string errorIfMissing)
    {
        return this switch
        {
            Some s => Result<T>.Ok(s.Value),
            None => Result<T>.Fail(errorIfMissing),
            _ => throw new InvalidOperationException()
        };
    }

    public T GetOrElse(T fallback)
    {
        return this switch
        {
            Some s => s.Value,
            None => fallback,
            _ => throw new InvalidOperationException()
        };
    }

    public R Match<R>(Func<T, R> onSome, Func<R> onNone)
    {
        return this switch
        {
            Some s => onSome(s.Value),
            None => onNone(),
            _ => throw new InvalidOperationException()
        };
    }
}
