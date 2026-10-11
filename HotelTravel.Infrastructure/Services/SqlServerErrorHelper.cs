
using Microsoft.Data.SqlClient;

namespace HotelTravel.Infrastructure.Services;

public static class SqlServerErrorHelper
{
    public static bool IsDeadlock(Exception exception)
    {
        if (exception is SqlException sqlException &&
            sqlException.Number == 1205)
        {
            return true;
        }

        return exception.InnerException != null &&
               IsDeadlock(exception.InnerException);
    }
}
