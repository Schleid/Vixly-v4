# Control theme behavior and login proportions

The document's inline script and the button's inline onclick handler were blocked by the existing `script-src 'self'` policy. Presentation behavior now lives in the same-origin `js/control-theme.js`, loaded before styles. The delegated button handler updates the document theme and accessible pressed state, remembers the preference where local storage is available, and still works when storage is blocked. The existing tenant-search keyboard shortcut is preserved. No security headers, authentication rules or backend logic changed.

The login keeps its 992×472 desktop footprint. Its columns now allocate about 57% to the blue security panel and 43% to the form (565.7px / 424.3px at the verified size). Error messages and smaller breakpoints may expand naturally to avoid clipping.

Verification: Control DLL build clean (`UseAppHost=false` avoided replacing the user's running executable); all 42 Control web tests passed. A browser loaded the real layout and script under the unchanged CSP: light → dark → reload remained dark; dark → light → reload remained light. The pressed state updated in both directions. Preview data/identity were synthetic, with no live account sign-in. The current running Control host must be restarted to load the rebuilt application document; it was not stopped by this task.
