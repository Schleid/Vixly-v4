# Public help capture fixture

Owner: Control team. Development-only tool for Public help Version 2.
May reference Public.Web and Contracts.Platform; never reads a database or contacts a payment/email provider. Must not be deployed as an application.

Runs the real Public components with fictional API responses. Binds to loopback only. Capture setup URLs set synthetic registration state, then redirect to the real page. They exist only in this executable, never in Public.Web.

`dotnet run --project build/HelpCapture --no-launch-profile`

Run `capture.cjs` with the bundled Node runtime and Playwright package available through NODE_PATH. It launches a separate headless browser with a temporary profile. Only page content is captured. Numbered outlines and arrows are added as a screenshot overlay without changing the application controls. No desktop/window capture API is used.

Fixtures illustrate screen controls, not real offers, accepted legal documents, payments or email delivery. Record workflow walkthrough separately. Stop the capture host before rebuilding it; it has separate output from the normal Public preview.
