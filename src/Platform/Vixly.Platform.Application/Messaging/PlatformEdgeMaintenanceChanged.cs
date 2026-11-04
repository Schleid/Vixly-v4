namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformEdgeMaintenanceChanged.cs
//
// PURPOSE:
// The outbox payload after a maintenance window was published, cancelled or ended early.
//
// RESPONSIBILITIES:
// - Name the window that changed
//
// IMPORTANT NOTES:
// - The window id is for the record; the handler always sends the whole current list
// =========================================================
public sealed record PlatformEdgeMaintenanceChanged(Guid WindowId);
