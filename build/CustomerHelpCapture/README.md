# Customer help Version 2 captures

Runs the actual Customer portal on loopback HTTPS port 7444 with fictional contract responses. `CaptureStartup` rewrites only the tool's host to the fictional company; none of its code is deployed in Customer.Web. The transport refuses writes and unknown calls. It uses an ephemeral signing key and data-protection keys, no personal browser profile and no real service or database.

For tour verification, pass `--tour` to the compiled fixture. This mode permits only an in-memory tour dismissal; package, shipment, payment and all other writes remain refused. Normal screenshot mode keeps the tour dismissed so the help images show the task controls. This fixture does not establish durable database behavior.

Run `dotnet run --project build/CustomerHelpCapture/CustomerHelpCapture.csproj`, then `node build/CustomerHelpCapture/capture.cjs` with Playwright and Edge available. Only browser document content is captured. Numbered SVG overlays mark actual controls without replacing their text. The output manifest records language, selectors and SHA-256. Close the capture server after verification.
