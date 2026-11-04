# Operations UI/UX brief for the Frontend team

| | |
|---|---|
| For | Frontend/UI team |
| From | Owner review of V3 and V4, 2026-10-20 |
| Applies to | Operations staff screens (`Vixly.Operations.Web`), the **Control console** (`Vixly.Platform.Web`, same rules; sign-in pages may stay Comfortable) and `Vixly.Shared.UI`; the customer portal follows sections 2, 3 and 9 |
| Changes | ADR-0013 F-4 (density), section 4 below |
| Status | **Draft for the owner's confirmation** |

## 1. The verdict

V3 is **beautiful despite its flaws**. Staff know it, and their eyes go to the right place. V4 must not feel far from it. But V3 wastes space, and V4's current screens do so even more: a simple name field sits in a huge control, every block has a banner, a label chip and an explanation, and people scroll for everything.

**The goal:** a calm, dense, fast workspace in Vixly's blue and white. A counter clerk serving a customer, or a worker scanning boxes, should see everything they need **without scrolling**, and never read the same information twice.

V3 shows **how the work is done**. Do not copy its screens. Understand the work, then design something better.

## 2. Non-negotiables

1. **No hero bars.** The page title lives in the top bar only.
   - V3 shows "Customer Shipments" in the top bar and then "Customer Lookup" in a banner below it: twice.
   - V4 must not do this anywhere, including on the home page.
2. **No decoration on work screens:**
   - no label chips such as "CUSTOMER OPERATIONS" or "INVOICE RECORDS";
   - no explanatory sentence under every heading ("Review shipment destination, service type…");
   - no "Total records / Workspace" tiles that repeat what the list already shows;
   - no illustrations or gradient banners.

   Help text appears only where a field needs it, as a short hint under the field or a tooltip.
3. **No scrolling for the main task.** At **1366 × 768** (the common counter screen) and at 1920 × 1080, the main task fits on one screen:
   - finding a customer, seeing their shipments and starting a new one;
   - adding a parcel;
   - scanning into a bundle;
   - taking a payment.

   Long content goes into **tabs**, not further down the page.
4. **Tabs, not stacked sections.** Follow the Prices page (`/staff/charges`), which splits its content into **Air · Ocean · Ground** tabs. Any page with several groups of content uses tabs or a segmented switch, not one long scroll of cards.
5. **Fields sized to their content.** Fields go in a multi-column grid: 4 to 6 columns on desktop, 2 on tablet, 1 on phone. Approximate widths:

   | Field | Width |
   |---|---|
   | Phone | ~16 characters |
   | Postal code | ~10 characters |
   | Weight or dimension | ~8 characters |
   | Name | ~30 characters |
   | Address line | ~40 characters |
   | Notes | the full row, 2 lines |

   A single field never gets its own full-width card. "Insurance: No/Yes" is a switch on the same line as other options.
6. **You see only what you are allowed to.**
   - Sections, tabs, links, buttons and columns a person's rights do not allow are **not rendered**: not disabled, not greyed, not "you need permission".
   - Read-only people see values, not disabled inputs.
   - Hiding is an extra layer; the server still checks every action.
7. **One page anatomy everywhere** (section 5). A worker who learns one list knows them all.

## 3. What to keep from V3

- **The colours and the shell:** blue and white, the dark navy sidebar, the company name and the person's name in the top bar.
- **A menu grouped by the job,** not by system concept (section 6).
- **The front desk starts from the customer:** look the customer up by phone or email, then do everything for that customer on the same page (section 7.1).
- **Counters that open their list in place.** On V3's customer page, "Active shipments 1" and "Pending invoices 1" open the list underneath. V4 does the same with tabs.
- **The live estimate beside the shipment form,** updating as fields change.
- **Status pills with a status age** ("Created · 129 days"). The age is how managers spot stuck work.
- **Label-over-value read-only fields** on detail pages, but compact: 4 to 6 per row, without a box around each one.

## 4. Density: change to ADR-0013 F-4

ADR-0013 made **Comfortable** the default density for forms, cards and navigation. For the Operations staff workspace, the default becomes **Compact**:

| Element | Compact (staff default) | Comfortable (customer portal, sign-in pages) |
|---|---|---|
| Input and select height | 32px | 40px |
| Table row | 36px, tabular figures | 44px |
| Gap between fields | 12px horizontal, 8px vertical | 16px / 12px |
| Card padding | 12 to 16px | 20 to 24px |
| Base text | 14px | 15 to 16px |
| Section heading | 15 to 16px semibold | 18px |
| Page header | at most 56px high | — |

On tablets and handheld scanners, touch targets stay at least 44px (ADR-0013). Only the station screens (receiving, inspection, bundle scanning) switch to touch size; the rest stays compact.

## 5. Page anatomy

**List page**, top to bottom with nothing in between:
1. **Filter bar** on one line: search box (focused on load), 2 to 4 filters, Apply and Clear.
2. **Status counts** as small chips that filter the list when clicked ("Open 12 · On hold 3 · Ready 40"), not tall cards.
3. **Actions row:** **+ New** (if allowed), Export or Print list, Refresh.
4. **Compact table** with a sticky header, key column first, status pill and status age, and row actions on the right as icon buttons or a "⋯" menu. Clicking the row opens the record.
5. **Pager:** Previous, page *n* of *m*, Next, page size.

**Record page**, top to bottom:
1. **Summary strip:** one or two lines with the record's identity and key facts (number, customer, route, status, amount) and its main actions on the right.
2. **Tabs** for everything else, each tab only if the person's rights allow it.
3. **"← Back to list"** keeps the filters the person came from.

**Station page** (receiving, inspection, bundle scan):
- one always-focused scan field;
- the result of the last scan, large and colour-coded;
- a short running list;
- nothing else.

Errors are spoken in words, not only colours.

**Forms:**
- labels above the fields;
- the field grid from section 2;
- the primary action bottom-left and Cancel beside it;
- the keyboard works throughout (Enter submits, Tab goes in reading order);
- errors under the field that caused them, with the typed values kept.

## 6. Menu

The menu is grouped by the job, and each person sees only their items. **The final list is settled with the owner screen by screen**; this is the direction:

| Group | Items |
|---|---|
| Front desk | Customers (the customer lookup is the front-desk home), Quotes, Tickets |
| Cashier | Payments |
| Warehouse | Inbound deliveries, Receiving, Inspection, Holds, Inventory (absorbs today's Parcels page), Bundles |
| Shipping | Bundles (ship manager), Dispatching |
| Setup | Locations, Services, Prices, Insurance, Inspection rules, Website, Settings |
| Administration | Staff, Roles, Devices, Company, Audit, Reports |

**Search** in the top bar takes any number (shipment, parcel, bundle, dispatch, tracking) or a customer's phone or email, and opens the matching record or timeline.

## 7. Blueprints for the key screens

These show **what goes where**, not the visual design. The design is yours.

### 7.1 Front desk: the customer hub

```
┌ Top bar: Galeo Logistics · Customers                          Henry Louis ▾ ┐
│ [ Phone or email ________________ ] [Find]                                   │
├──────────────────────────────────────────────────────────────────────────────┤
│ Jean Pierre · C00042 · (561) 555-0102 · jean@… · Active        [+ Shipment]  │
├──────────────────────────────────────────────────────────────────────────────┤
│ [Shipments 3] [Packages 2] [Invoices 1] [Tickets 0] [Profile]   ← by rights  │
│ ┌ Shipment ─┬ Destination ┬ Service ┬ Recipient ┬ Parcels ┬ Status ┬ ⋯ ┐     │
│ │ SH26000123 │ Cayes, pickup│ Air std │ Marie P. │ 1 of 3   │ Open   │ ⋯ │     │
│ └───────────┴─────────────┴─────────┴──────────┴─────────┴────────┴───┘     │
└──────────────────────────────────────────────────────────────────────────────┘
```

- Opening a shipment shows it **inside the hub**: shipment details in one compact grid, the parcel list, and **+ Parcel**, with the live estimate on the right.
- Parcel entry has weight, length, width, height, description and value on **one row**, and **Add** keeps focus for the next parcel.
- "1 of 3" shows the count the customer declared. When the last parcel is entered, the shipment finalizes and the amount and invoice appear.
- The clerk never needs to scroll.

### 7.2 Cashier

The same lookup and summary strip. The **Invoices** tab lists number, shipment, status, total, balance and due date, with **Pay** on each row. Pay opens a small panel: amount (defaults to the balance), method and reference. It confirms, then shows the receipt with Print.

### 7.3 Setup pages

Setup pages follow the Prices page: **tabs per mode or area, compact tables with inline editing**, and the form opening beside or above the table, not on a separate long page.

## 8. Checks before a screen is called done

1. Screenshots at **1366 × 768, 1920 × 1080, 768 (tablet) and 375 (phone)**, in **en, fr and es**. At 1366 × 768 the main task is fully visible without scrolling.
2. No hero bar, label chip, repeated title or decorative illustration on a work screen.
3. Every field's width matches section 2, and no single field has its own card.
4. Signed in as each role in `DEVELOPMENT-DEMO-USERS.md`, the screen shows only what that role may do: nothing disabled, nothing greyed.
5. The keyboard alone completes the main task, and the scan field keeps focus on station pages.
6. Light and dark mode, and contrast checked (WCAG 2.2 AA).
7. **The owner reviews the screens before they merge.**

## 9. Order of work

1. **Shell and density:** compact tokens, top bar with search, the job-based menu, removal of hero bars, label chips and explanation lines across existing pages.
2. **Shared primitives:**
   - the list-page anatomy (filter bar, count chips, actions row, compact table with status age, pager);
   - the record page (summary strip and tabs);
   - the multi-column field grid with field widths.
3. **Front desk customer hub and cashier** (7.1 and 7.2). The backend reshapes these with you, because the counter flow is being redesigned with the owner now.
4. **The remaining pages** adopt the anatomy one by one, each reviewed by the owner.

## 10. The styling inventory: what to work on

[UI-STYLING-INVENTORY.md](UI-STYLING-INVENTORY.md) lists every screen and its status. **It is the FE team's work list.**
- **Start with** rows marked *Needs styling* or *Restyle*, in the order of section 9.
- **Do not style** rows marked *On hold*: those screens are being redesigned with the owner and will change.
- **Move each row** to *In progress*, then *Styled*, with the date and a line on what you did.
- **Mark *Approved*** only after the owner's review.
- **The backend team** adds a row (*Needs styling*) every time it builds a screen. When it changes a styled screen, it marks the row *Changed* and says what changed.
- **Keep the file up to date** in the same commit as the work. It is how the owner sees, at any moment, what is waiting on whom.

Still being settled with the owner, so don't design these yet: dispatching, pricing changes, the reports content, and exactly which customer sections each role sees.
