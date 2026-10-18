namespace HotelTravel.Web.Services.Api;

// API ilə əlaqədə yaranan xətalar (ApiExceptionFilter tutur)
public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(string message, int statusCode = 500)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

// 401: token yoxdur və ya vaxtı keçib -> yenidən login
public class ApiUnauthorizedException : ApiException
{
    public ApiUnauthorizedException()
        : base("Your session has expired. Please sign in again.", 401)
    {
    }
}

// 403: icazə yoxdur
public class ApiForbiddenException : ApiException
{
    public ApiForbiddenException()
        : base("You do not have permission for this action.", 403)
    {
    }
}

// API işləmir / əlçatan deyil
public class ApiUnavailableException : ApiException
{
    public ApiUnavailableException(Exception inner)
        : base("The API is not reachable. Make sure HotelTravel.API is running.", 503)
    {
        InnerError = inner;
    }

    public Exception InnerError { get; }
}
