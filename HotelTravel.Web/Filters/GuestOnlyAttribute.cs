using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HotelTravel.Web.Filters;

/// <summary>
/// Admin və otel partnyoru işçi hesablarıdır: rezervasiya etmir, "Rezervasiyalarım" / "Saxlananlar" yoxdur.
/// Belə hesabla bu səhifə açılanda izah səhifəsi göstərilir (AJAX sorğusuna 403).
/// Qonaq kimi yoxlamaq üçün ayrıca adi hesabla daxil olmaq lazımdır.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class GuestOnlyAttribute : ActionFilterAttribute
{
    public static bool IsStaff(System.Security.Claims.ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true &&
        (user.IsInRole("Admin") || user.IsInRole("HotelPartner"));

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!IsStaff(context.HttpContext.User))
            return;

        var request = context.HttpContext.Request;
        var isAjax =
            request.Headers.XRequestedWith == "XMLHttpRequest" ||
            request.Headers.Accept.ToString().Contains("application/json");

        if (isAjax)
        {
            context.Result = new JsonResult(new { success = false, message = "Staff accounts cannot make bookings." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        context.Result = context.Controller is Controller controller
            ? new ViewResult
            {
                ViewName = "~/Views/Shared/StaffNotice.cshtml",
                ViewData = controller.ViewData,
                TempData = controller.TempData
            }
            : new ViewResult { ViewName = "~/Views/Shared/StaffNotice.cshtml" };
    }
}
