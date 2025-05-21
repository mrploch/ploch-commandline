using System;

namespace Ploch.CommandLine.UseCases.Models;

public class ErrorModel(string message, string errorCode, Type? exceptionType)
{
    public string Message { get; } = message;

    public string ErrorCode { get; } = errorCode;

    public Type? ExceptionType { get; } = exceptionType;
}
