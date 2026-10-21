using System.Net;

namespace HotelTravel.Application.Services;

// Hesab məktubları (şifrə bərpası). İstifadəçi məlumatı HtmlEncode olunur.
public static class AccountEmailTemplates
{
    public const string PasswordResetType = "PasswordReset";
    public const int LinkValidHours = 24;

    public static string ResetLink(string siteUrl, string email, string encodedToken)
        => $"{siteUrl.TrimEnd('/')}/Account/ResetPassword" +
           $"?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(encodedToken)}";

    // Admin jurnalı üçün: link görünmür (admin başqasının şifrəsini dəyişə bilməsin)
    public static (string Subject, string Html) PasswordResetForLog(string firstName)
    {
        var (subject, html) = PasswordReset(firstName, "#");

        html = html.Replace(
            @"<a href=""#""",
            @"<a title=""The personal reset link is hidden in the log for security""")
            .Replace(">Reset password</a>", ">Reset password (link hidden for security)</a>");

        return (subject, html);
    }

    public static (string Subject, string Html) PasswordReset(string firstName, string link)
    {
        const string subject = "Reset your Tripio password";

        var name = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(firstName) ? "there" : firstName.Trim());
        var href = WebUtility.HtmlEncode(link);

        var html = $@"<!doctype html>
<html><head><meta charset=""utf-8""><title>{subject}</title></head>
<body style=""margin:0;background:#F3EEE5;font-family:Segoe UI,Arial,sans-serif;color:#1F2421"">
  <div style=""max-width:600px;margin:0 auto;padding:28px 16px"">
    <div style=""font-family:Georgia,serif;font-size:26px;font-weight:700;margin-bottom:18px"">&#11042; Tripio</div>
    <div style=""background:#fff;border-radius:18px;padding:28px"">
      <p style=""margin:0 0 16px"">Hi {name},</p>
      <p style=""margin:0 0 20px"">We received a request to reset your password. Click the button below to choose a new one.</p>
      <p style=""margin:0 0 24px"">
        <a href=""{href}"" style=""background:#1F2421;color:#fff;text-decoration:none;padding:12px 22px;border-radius:999px;font-weight:700;display:inline-block"">Reset password</a>
      </p>
      <p style=""margin:0 0 8px;color:#6b7280;font-size:13px"">The link is valid for {LinkValidHours} hours and can be used once.</p>
      <p style=""margin:0;color:#6b7280;font-size:13px"">If you didn't ask for this, you can ignore this email — your password stays the same.</p>
    </div>
    <p style=""color:#9ca3af;font-size:12px;text-align:center;margin-top:18px"">This email was sent by Tripio. Please do not reply.</p>
  </div>
</body></html>";

        return (subject, html);
    }
}
