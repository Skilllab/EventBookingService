using System;

using Microsoft.AspNetCore.Mvc;

namespace EventForge.ExceptionMiddleware;

public class ExceptionHandlingOptions
{
    public Func<Exception, (int StatusCode, ProblemDetails ProblemDetails)> ExceptionHandler { get; set; }
}
