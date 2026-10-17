# Development Rules

This directory is the canonical home for repository-wide engineering rules and coding conventions.

- [abpDevRules.md](abpDevRules.md) is the main rule set, adapted for this repository.
- The solution currently uses conventional .NET 8 Core, Application, Infrastructure, CLI, and test projects; it is not an ABP Framework application. Apply ABP-specific instructions only if that architecture is deliberately adopted later.
- Use XML flowerboxing on every class/type, constructor, and method. State what it does and when it is called.
- Preserve clean layer boundaries, deterministic/testable calculations, explicit data freshness, no-look-ahead replay, and source-study parity testing.
- Put future rule documents in this directory and link them here to keep one discoverable source of truth.
