using System;

namespace Ploch.Tools.SystemProfiles.UseCases.Models;

public class ErrorModel(string message, string errorCode, Type? exceptionType)
{
    public string Message { get; } = message;

    public string ErrorCode { get; } = errorCode;

    public Type? ExceptionType { get; } = exceptionType;
}