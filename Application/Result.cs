namespace EGovServices.Application.Common;

// ─── Result<T> — للعمليات التي تُرجع قيمة ───────────────────────────────────
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    { IsSuccess = isSuccess; Value = value; Error = error; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure) =>
        IsSuccess && Value is not null ? onSuccess(Value) : onFailure(Error ?? "Unknown error");
}

// ─── Result — للعمليات التي لا تُرجع قيمة (مثل AddProcessingNote) ────────────
public sealed class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    private Result(bool isSuccess, string? error) { IsSuccess = isSuccess; Error = error; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    // ✅ Match للـ Result العادي — يُستخدم في الـ Controller
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error ?? "Unknown error");
}
