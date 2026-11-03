# Print helper (Vixly Print)

Slice 12E (owner H-6). A small phone app that only prints: it joins a warehouse with the one-time code of the Printers page
and prints that phone's labels on the Bluetooth printer next to it. Scanning, receiving and delivering stay in the browser.

## Build

- Needs the .NET 10 MAUI workloads: `dotnet workload install maui-android maui-ios` (iOS also needs a Mac with Xcode).
- Not part of `Vixly.Operations.slnx` (CI does not have the workloads). Open `Vixly.PrintHelper.slnx`.
- The logic is in `Vixly.PrintHelper.Core`, built and tested with the rest (`Vixly.PrintHelper.Core.Tests`).

## Name and stores

- Default name **Vixly Print**, app id `ltd.techcrafters.vixlyprint`, published by Tech Crafters.
- A white-label company's own copy: `dotnet publish -f net10.0-android -p:PrintHelperAppName="Galeo Print" -p:PrintHelperAppId=net.galeologistics.print`
  (and its own icon in `Resources/AppIcon`). The app shows its own name only.
- The Printers page names the app from the setting `OperationsPrintHelper:AppName`.

## Printers

- Bluetooth Low Energy, Zebra Link-OS "Parser" service (ZQ500/ZQ600 series, ZD with BLE). Labels are ZPL, the same as the dock printers.
- Classic Bluetooth on iPhone needs Apple's MFi program and is not used. On Android, a printer without BLE could be added later (SPP).

## Flow

1. Printers page (warehouse) › *Add a phone* › a name › a QR code valid 10 minutes, once.
2. The app scans it (or the address and code are typed), joins, and keeps its key in secure storage.
3. *Find printers nearby* › choose one › it appears on the Printers page and can be chosen like any printer.
4. While the app is open, it takes that printer's labels every 3 seconds. *Stop* on the Printers page ends it; the app then asks to join again.
