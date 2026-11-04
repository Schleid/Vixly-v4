namespace Vixly.Operations.Domain.Texts;

// =========================================================
// FILE: OperationsTextTemplate.cs
//
// PURPOSE:
// A WhatsApp message template the company had approved by Meta in Twilio, for one notice (Slice 9).
//
// RESPONSIBILITIES:
// - Keep the notice and the template’s content id (HX...)
//
// IMPORTANT NOTES:
// - WhatsApp only lets a business start a conversation with an approved template; its variables are {{1}} the company, {{2}} the text, {{3}} the link
// =========================================================
public sealed class OperationsTextTemplate
{
    private OperationsTextTemplate()
    {
        ContentSid = string.Empty;
    }

    public OperationsTextTemplate(OperationsTextNotices notice, string contentSid)
    {
        (Notice, ContentSid) = (notice, contentSid);
    }

    public OperationsTextNotices Notice { get; private set; }

    public string ContentSid { get; private set; }
}
