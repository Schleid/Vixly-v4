namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsPortalLinks.cs
//
// PURPOSE:
// Builds links to a tenant portal.
//
// RESPONSIBILITIES:
// - Make the absolute address of a page on a tenant host
//
// IMPORTANT NOTES:
// - From configuration (OperationsLinks); the slug comes from Platform
// =========================================================
public interface IOperationsPortalLinks
{
    string Page(string slug, string pathAndQuery);
}
