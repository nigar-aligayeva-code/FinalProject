using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;

namespace HotelTravel.Web.Localization;

/// <summary>
/// Səhifə göstərilməzdən əvvəl istifadəçiyə gedən mesajları seçilmiş dilə çevirir:
/// ModelState xətaları (API-dən gələn "Room not found." və s.), TempData və ViewBag mesajları.
/// Belə ki, controller-lərdə hər mesajı ayrıca tərcümə etməyə ehtiyac qalmır.
/// Tərcümə tapılmasa mətn olduğu kimi (ingiliscə) qalır.
/// </summary>
public class LocalizeMessagesFilter : IResultFilter
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizeMessagesFilter(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        // Yalnız səhifə (view) göstəriləndə; redirect və JSON-a toxunmuruq
        if (context.Result is not (ViewResult or PartialViewResult))
            return;

        LocalizeModelState(context.ModelState);

        if (context.Controller is Controller controller)
        {
            LocalizeTempData(controller);
            LocalizeViewData(controller);
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private void LocalizeModelState(ModelStateDictionary modelState)
    {
        foreach (var entry in modelState.Values)
        {
            if (entry.Errors.Count == 0)
                continue;

            var errors = entry.Errors.ToList();
            entry.Errors.Clear();

            foreach (var error in errors)
            {
                if (string.IsNullOrEmpty(error.ErrorMessage))
                    entry.Errors.Add(error);
                else
                    entry.Errors.Add(_localizer[error.ErrorMessage].Value);
            }
        }
    }

    private void LocalizeTempData(Controller controller)
    {
        foreach (var key in controller.TempData.Keys.ToList())
        {
            // Peek: oxunmuş kimi işarələmir, mesaj view-da göstərilənə qədər qalır
            if (controller.TempData.Peek(key) is string text && !string.IsNullOrWhiteSpace(text))
                controller.TempData[key] = _localizer[text].Value;
        }
    }

    private void LocalizeViewData(Controller controller)
    {
        foreach (var key in controller.ViewData.Keys.ToList())
        {
            var isMessage =
                key.EndsWith("Message", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("Error", StringComparison.OrdinalIgnoreCase);

            if (isMessage && controller.ViewData[key] is string text && !string.IsNullOrWhiteSpace(text))
                controller.ViewData[key] = _localizer[text].Value;
        }
    }
}
