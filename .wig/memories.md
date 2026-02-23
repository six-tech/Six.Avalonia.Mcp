# 2026-02-17 20:06:56 UTC | Run: avalonia-runtime-mcp-server-for-ai-agents-with-i (completed) | 45/45 tasks
Done! I've extracted and added 5 high-level, generalizable learnings to your memory:

1. **Headless Testing Gap with GUI Frameworks** — Identifies the practical boundary between unit testing (DOM/events) and integration testing (rendering) in headless environments.

2. **Preview SDKs Require Attribute Archaeology** — Notes that early-access SDKs may lack expected properties; fall back to standard .NET attributes (like `Description`) that the SDK reads at runtime.

3. **Non-Implementable Interfaces Are a Code Smell** — Flags APIs that hide implementation (like Avalonia's `IPointer`) and recommends building adapters early to avoid scattering workarounds.

4. **Integration Tests with Mocking Achieves High Coverage Fast** — Documents the practical strategy that combined unit + integration tests to jump from 47.7% → 95.5% coverage by testing delegation contracts rather than reimplementing framework internals.

5. **NDJSON Protocol Differs From HTTP Framing** — Highlights a subtle but critical detail: STDIO transports use line-delimited messages, not Content-Length headers, which affects serialization debugging.

These are stored in your MEMORY.md and will be available for future development runs on different specs.
Detailed memories: .wig/runs/avalonia-runtime-mcp-server-for-ai-agents-with-i/memories.md

