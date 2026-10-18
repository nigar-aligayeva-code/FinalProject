using HotelTravel.Web.Services.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HotelTravel.Web.Filters;

// API xətalarını istifadəçi üçün səliqəli nəticəyə çevirir
public class ApiExceptionFilter : IAsyncExceptionFilter
{
    private readonly IModelMetadataProvider _metadataProvider;
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(
        IModelMetadataProvider metadataProvider,
        ILogger<ApiExceptionFilter> logger)
    {
        _metadataProvider = metadataProvider;
        _logger = logger;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        var http = context.HttpContext;

        switch (context.Exception)
        {
            // Token vaxtı keçib -> çıxış və yenidən login
            case ApiUnauthorizedException:
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                context.Result = new RedirectToActionResult(
                    "Login",
                    "Account",
                    new { area = "", returnUrl = http.Request.Path + http.Request.QueryString });

                context.ExceptionHandled = true;
                break;

            case ApiForbiddenException:
                context.Result = new RedirectToActionResult(
                    "AccessDenied",
                    "Account",
                    new { area = "" });

                context.ExceptionHandled = true;
                break;

            // API işləmir
            case ApiUnavailableException ex:
                _logger.LogError(ex.InnerError, "API is not reachable.");
                context.Result = ErrorView(context, "ApiUnavailable", ex.Message);
                context.ExceptionHandled = true;
                break;

            // API xəta qaytardı (məs. 400/500)
            case ApiException ex:
                _logger.LogWarning(ex, "API error {Status}: {Message}", ex.StatusCode, ex.Message);
                context.Result = ErrorView(context, "ApiError", ex.Message);
                context.ExceptionHandled = true;
                break;
        }
    }

    private ViewResult ErrorView(ExceptionContext context, string viewName, string message)
        => new()
        {
            ViewName = "~/Views/Shared/" + viewName + ".cshtml",
            ViewData = new ViewDataDictionary(_metadataProvider, context.ModelState)
            {
                ["ErrorMessage"] = message
            }
        };
}
