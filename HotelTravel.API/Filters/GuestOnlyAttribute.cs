using HotelTravel.Application.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HotelTravel.API.Filters;

/// <summary>
/// Admin və otel partnyoru işçi hesablarıdır: rezervasiya etmir.
/// Sayt (Web) bunu artıq göstərmir; bu filtr eyni qaydanı API-də də (Swagger-dən gələn sorğuya) tətbiq edir.
/// Qonaq (token-siz) və adi istifadəçi üçün heç nə dəyişmir.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class GuestOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated == true &&
            (user.IsInRole("Admin") || user.IsInRole("HotelPartner")))
        {
            context.Result = new ObjectResult(ApiResultDto.Fail("Staff accounts cannot make bookings. Use a guest account."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
