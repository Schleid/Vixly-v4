namespace Vixly.Shared.UI.Primitives;

// =========================================================
// FILE: VxPasswordInput.cs
//
// PURPOSE:
// Password specialization of the shared text input.
//
// RESPONSIBILITIES:
// - Render a password input while inheriting the six binding/accessibility parameters
//
// IMPORTANT NOTES:
// - Presentation only; no credential handling or authentication logic
// - Keeps the component parameter budget unchanged
// =========================================================
public sealed class VxPasswordInput : VxTextInput
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Type = "password";
    }
}
