using gAPI.Core.Dtos;
using System.Diagnostics;

namespace gAPI.Core.Helpers;

public class RemoteException : Exception
{
    public RemoteException(ExceptionDto exception) :
        base(
            exception.Message,
            exception.InnerMessage == null ? null : new Exception(exception.InnerMessage))
    {
        StackTrace = exception.StackTrace;
    }

    //public RemoteException(string? message, string? innerException) :
    //    base(
    //        message,
    //        innerException == null ? null : new Exception(innerException))
    //{
    //}
    public RemoteException(string? message, string? innerException, string? stackTrace) :
        base(
            message,
            innerException == null ? null : new Exception(innerException))
    {
        StackTrace = stackTrace;
    }

    public override string? StackTrace { get; }
}
