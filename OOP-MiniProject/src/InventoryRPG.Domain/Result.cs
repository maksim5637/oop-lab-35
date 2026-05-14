namespace InventoryRPG.Domain;

/// <summary>
/// Result&lt;T&gt; — замість винятків для очікуваних ситуацій.
/// Використовується скрізь де операція може не вдатися з відомої причини.
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T?   Value     { get; }
    public string Error   { get; }

    private Result(bool isSuccess, T? value, string error)
    {
        IsSuccess = isSuccess;
        Value     = value;
        Error     = error;
    }

    public static Result<T> Ok(T value)       => new(true,  value, string.Empty);
    public static Result<T> Fail(string error) => new(false, default, error);

    public override string ToString() =>
        IsSuccess ? $"Ok({Value})" : $"Fail({Error})";
}
