using Microsoft.Data.SqlClient;

namespace HotelTravel.Infrastructure.Services;

public static class SqlServerErrorHelper
{
    public static bool IsDeadlock(Exception exception)
    {
        if (exception is SqlException sqlException &&
            IsDeadlockErrorNumber(sqlException.Number))
        {
            return true;
        }

        return exception.InnerException != null &&
               IsDeadlock(exception.InnerException);
    }

    public static bool IsDeadlockErrorNumber(int errorNumber)
    {
        return errorNumber == 1205;
    }
}