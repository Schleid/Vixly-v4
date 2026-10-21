using Vixly.Public.Web.Hosting;

namespace Vixly.Public.Web.Features.Registration;

// =========================================================
// FILE: RegistrationSteps.cs
//
// PURPOSE:
// Which sign-up page belongs to each registration step Platform reports (Slice 2).
//
// RESPONSIBILITIES:
// - Map a step code to its page
//
// IMPORTANT NOTES:
// - Platform owns the step; the site never guesses it
// =========================================================
public static class RegistrationSteps
{
    public static string PageFor(string step)
    {
        string language = PublicLanguage.Current;

        return step switch
        {
            "pending_email_verification" => PublicLanguage.Page(language, "register/email"),
            "pending_plan_selection" => PublicLanguage.Page(language, "register/plan"),
            "pending_payment" => PublicLanguage.Page(language, "register/pay"),
            "pending_account_setup" or "pending_first_device" or "completed" => PublicLanguage.Page(language, "register/done"),
            _ => PublicLanguage.Page(language, "register") + "?ended=1"
        };
    }
}
