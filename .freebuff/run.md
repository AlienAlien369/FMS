# FMS Preview — How to Run

## Reproduce uncommitted artifacts
No special setup needed — `preview.html` is a self-contained standalone HTML file with all CSS inline.

## Run the server
No server needed — the file is served directly by the Freebuff preview system via `register_preview` with `htmlPath`.

To re-register manually if the preview stops:
1. Open the Preview tab in the thread.
2. The agent calls `register_preview` with `htmlPath` pointing to `preview.html` at the workspace root.
