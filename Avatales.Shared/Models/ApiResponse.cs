namespace Avatales.Shared.Models;

/// <summary>
/// Standard API Response für CQRS Commands ohne Rückgabewert
/// </summary>
public class ApiResponse
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public List<string> Errors { get; private set; } = new();
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    protected ApiResponse(bool isSuccess, string message, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        Errors = errors ?? new List<string>();
    }

    // Factory Methods für Success
    public static ApiResponse Success(string message = "Operation completed successfully")
    {
        return new ApiResponse(true, message);
    }

    // Factory Methods für Failure
    public static ApiResponse FailureResult(string message, params string[] errors)
    {
        return new ApiResponse(false, message, errors.ToList());
    }

    public static ApiResponse FailureResult(string message, List<string> errors)
    {
        return new ApiResponse(false, message, errors);
    }

    // Validation Failure
    public static ApiResponse ValidationFailure(List<string> validationErrors)
    {
        return new ApiResponse(false, "Validation failed", validationErrors);
    }

    // Not Found
    public static ApiResponse NotFound(string resourceName = "Resource")
    {
        return new ApiResponse(false, $"{resourceName} not found");
    }

    // Unauthorized
    public static ApiResponse Unauthorized(string message = "Unauthorized access")
    {
        return new ApiResponse(false, message);
    }

    // Forbidden
    public static ApiResponse Forbidden(string message = "Access forbidden")
    {
        return new ApiResponse(false, message);
    }
}

/// <summary>
/// Standard API Response für CQRS Commands/Queries mit Rückgabewert
/// </summary>
public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; private set; }

    private ApiResponse(bool isSuccess, string message, T? data = default, List<string>? errors = null)
        : base(isSuccess, message, errors)
    {
        Data = data;
    }

    // Factory Methods für Success mit Data
    public static ApiResponse<T> SuccessResult(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>(true, message, data);
    }

    // Factory Methods für Failure
    public static new ApiResponse<T> FailureResult(string message, params string[] errors)
    {
        return new ApiResponse<T>(false, message, default, errors.ToList());
    }

    public static new ApiResponse<T> FailureResult(string message, List<string> errors)
    {
        return new ApiResponse<T>(false, message, default, errors);
    }

    // Validation Failure
    public static new ApiResponse<T> ValidationFailure(List<string> validationErrors)
    {
        return new ApiResponse<T>(false, "Validation failed", default, validationErrors);
    }

    // Not Found
    public static new ApiResponse<T> NotFound(string resourceName = "Resource")
    {
        return new ApiResponse<T>(false, $"{resourceName} not found");
    }

    // Unauthorized
    public static new ApiResponse<T> Unauthorized(string message = "Unauthorized access")
    {
        return new ApiResponse<T>(false, message);
    }

    // Forbidden
    public static new ApiResponse<T> Forbidden(string message = "Access forbidden")
    {
        return new ApiResponse<T>(false, message);
    }

    // Implicit Conversion von T zu ApiResponse<T>
    public static implicit operator ApiResponse<T>(T data)
    {
        return SuccessResult(data);
    }
}

/// <summary>
/// Spezielle Response für Batch-Operationen
/// </summary>
public class BatchApiResponse<T> : ApiResponse<List<T>>
{
    public int TotalProcessed { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public List<BatchError> BatchErrors { get; private set; } = new();

    private BatchApiResponse(
        bool isSuccess,
        string message,
        List<T>? data = null,
        int totalProcessed = 0,
        int successCount = 0,
        int failureCount = 0,
        List<BatchError>? batchErrors = null,
        List<string>? errors = null)
        : base(isSuccess, message, data, errors)
    {
        TotalProcessed = totalProcessed;
        SuccessCount = successCount;
        FailureCount = failureCount;
        BatchErrors = batchErrors ?? new List<BatchError>();
    }

    public static BatchApiResponse<T> BatchSuccess(
        List<T> data,
        int totalProcessed,
        int successCount,
        int failureCount = 0,
        List<BatchError>? batchErrors = null)
    {
        var message = failureCount > 0
            ? $"Batch completed with {successCount} successes and {failureCount} failures"
            : $"Batch completed successfully. {successCount} items processed";

        return new BatchApiResponse<T>(
            isSuccess: failureCount == 0,
            message: message,
            data: data,
            totalProcessed: totalProcessed,
            successCount: successCount,
            failureCount: failureCount,
            batchErrors: batchErrors);
    }

    public static BatchApiResponse<T> BatchFailure(
        string message,
        int totalProcessed = 0,
        List<BatchError>? batchErrors = null,
        List<string>? generalErrors = null)
    {
        return new BatchApiResponse<T>(
            isSuccess: false,
            message: message,
            totalProcessed: totalProcessed,
            batchErrors: batchErrors,
            errors: generalErrors);
    }
}

/// <summary>
/// Einzelner Batch-Fehler
/// </summary>
public class BatchError
{
    public int ItemIndex { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();

    public BatchError(int itemIndex, string itemId, string errorMessage, List<string>? details = null)
    {
        ItemIndex = itemIndex;
        ItemId = itemId;
        ErrorMessage = errorMessage;
        Details = details ?? new List<string>();
    }
}