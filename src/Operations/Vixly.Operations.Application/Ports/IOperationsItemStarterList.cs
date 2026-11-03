using Vixly.Operations.Application.Items;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsItemStarterList.cs
//
// PURPOSE:
// The starter item catalogue every company receives once (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Give the everyday courier items with their customs codes
//
// IMPORTANT NOTES:
// - Data, not code: a file shipped with the application
// =========================================================
public interface IOperationsItemStarterList
{
    IReadOnlyList<OperationsStarterItem> Items { get; }
}
