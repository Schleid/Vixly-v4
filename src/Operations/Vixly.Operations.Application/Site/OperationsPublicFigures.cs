namespace Vixly.Operations.Application.Site;

// =========================================================
// FILE: OperationsPublicFigures.cs
//
// PURPOSE:
// How a real count is shown on the company's website (owner, 2026-10-30): "8,500+ packages delivered".
//
// RESPONSIBILITIES:
// - Round a count down to a figure that stays true until the next count
// - Hide a count too small to be worth showing
//
// IMPORTANT NOTES:
// - Never rounded up and never invented: the figure shown is always at most the real count
// =========================================================
public static class OperationsPublicFigures
{
    public const int DeliveredMinimum = 500;

    public static int? Delivered(int count)
    {
        if (count < DeliveredMinimum)
        {
            return null;
        }

        int step = count < 1_000 ? 50 : count < 10_000 ? 100 : 1_000;
        return count / step * step;
    }
}
