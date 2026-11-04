# Vixly.PrintAgent

The dock print program: a small Windows service on a warehouse computer that sends ID labels to the warehouse's label printers (Slice 3B). It asks the company's Operations site for waiting labels every few seconds and sends each one to its printer, over the network (raw port 9100) or to a printer installed on that computer.

| | |
|---|---|
| Owner | Operations team |
| Build stage | 3 |
| May reference | Vixly.Contracts.Operations |
| Must not reference | Any Operations project; the database; anything a browser uses. |

## Setup on the dock computer

1. In the staff site, open **Locations → the warehouse → Printers**, add the printers, then **register a print program**. Its key is shown once.
2. Publish this project (`dotnet publish -c Release -r win-x64`) and copy it to the computer.
3. Set, as service environment variables (never in a shared file):
   - `PrintAgent__OperationsUrl`: the company's staff address, for example `https://acme.example.com`;
   - `PrintAgent__AgentKey`: the key from step 1.
4. Install it as a service: `sc.exe create VixlyPrintAgent binPath= "C:\VixlyPrintAgent\Vixly.PrintAgent.exe" start= auto`, then start it.
5. Use **Print a test label** on the Printers page.

A label is printed at most once, and only within two minutes of the scan: a late label would end up on the wrong package. When a label does not come out, the clerk scans the package again. Stopping a print program on the Printers page makes its key useless at once.

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md).
