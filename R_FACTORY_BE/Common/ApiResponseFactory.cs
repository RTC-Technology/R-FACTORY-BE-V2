using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace R_FACTORY_BE.Common;

public static class ApiResponseFactory
{
    public static IActionResult Success<T>(
      T data,
      string? message = null,
      HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new ObjectResult(new ApiResponse<T>
        {
            Success = true,
            StatusCode = (int)statusCode,
            Message = message,
            Data = data
        })
        {
            StatusCode = (int)statusCode
        };
    }

    public static IActionResult Fail(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        object? errors = null)
    {
        return new ObjectResult(new ApiResponse<object>
        {
            Success = false,
            StatusCode = (int)statusCode,
            Message = message,
            Errors = errors
        })
        {
            StatusCode = (int)statusCode
        };
    }
}