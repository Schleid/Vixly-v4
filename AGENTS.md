# AGENTS.md

Entry point for any engineer or AI assistant working in this repository.

1. Read `docs/global/VIXLY-GLOBAL-STANDARD.md` before writing or changing code. It is the single global document and it wins over every other document.
2. Read the document for the plane you are working in: `docs/<plane>/`.
3. Every project folder has a README stating its owner, what it may reference and what it must not reference.
4. If a rule is wrong or missing, propose an ADR in `docs/decisions/`. Do not work around it.
5. Entity classes carry their platform (`PlatformInvoice`, `OperationsInvoice`, `CustomerInvoice`); tables sit in the plane's schema without the prefix (`platform.Invoices`). Planes never reference each other's internals or tables; they talk through `src/Contracts`.
