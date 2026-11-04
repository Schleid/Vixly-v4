namespace Vixly.Operations.Application.Site;

// =========================================================
// FILE: OperationsTestimonialView.cs
//
// PURPOSE:
// Which testimonials the company looks at (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Name the views: all, new customer reviews, published, hidden, and those staff entered
//
// IMPORTANT NOTES:
// - New means a customer review the company has not published or hidden since the customer last sent it
// =========================================================
public enum OperationsTestimonialView
{
    All = 0,
    New = 1,
    Published = 2,
    Hidden = 3,
    Staff = 4
}
