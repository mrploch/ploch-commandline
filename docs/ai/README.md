# AI knowledge base — ploch-commandline

Verified, agent-oriented documentation of how this repository works and how development is done
in it. The entry point is [`CLAUDE.md`](../../CLAUDE.md) at the repository root; this folder holds
the detail.

| Field | Value |
|---|---|
| Last verified | 2026-10-05, against commit `6a2c931` |
| Standard version | 1 |

## Documents

| File | Answers | Changes when |
|---|---|---|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | What are the parts and how do they interact? | Public API or flow changes |
| [`TECH-STACK.md`](TECH-STACK.md) | What is it built with, and how is it built and shipped? | Dependencies, CI or release change |
| [`CONVENTIONS.md`](CONVENTIONS.md) | How is code written, committed and reviewed here? | Process or style changes |
| [`BACKLOG.md`](BACKLOG.md) | What is open, planned and owed? | Often; it is a snapshot |
| [`AGENT-CONFIG.md`](AGENT-CONFIG.md) | What agent configuration exists and what should change? | Agent configuration changes |

## The standard

The same set is used in every repository of the organisation, so an agent knows where to look
without searching.

- **Fixed names and location.** `CLAUDE.md` at the root, the five documents above in `docs/ai/`.
  A repository may add a document; it does not rename or drop one. A document that does not
  apply keeps its file and says so in one line.
- **Fixed sections.** Each of the five documents has its own list of numbered top-level sections,
  kept in the same order in every repository. An empty section says "None" rather than
  disappearing. `CLAUDE.md` and this index have fixed, unnumbered headings.
- **Verification stamp.** Every document starts with the date and commit it was verified against.
- **Evidence over assertion.** A claim that is not obvious cites a file path, a command that was
  run, or a tracker link. Anything not checked is marked `UNVERIFIED`.
- **Describe what is, not what should be.** Where practice differs from a written rule, the
  document records the practice and names the difference.
- **No duplication of human documentation.** Link to `README.md`, `CONTRIBUTING.md` and the guides
  instead of copying them.
- **Written for a public repository.** No credentials, no personal paths, no account identifiers,
  no machine-specific ports.
- **Format.** Prose wrapped at 120 characters (table rows may be longer), British English.

## Keeping it current

Update the relevant document in the same pull request as the change that makes it wrong:

| Change | Update |
|---|---|
| New project, public type or changed flow | `ARCHITECTURE.md`, and the layout in `CLAUDE.md` |
| Dependency, SDK, workflow or release change | `TECH-STACK.md` |
| New or changed convention | `CONVENTIONS.md` |
| Agent configuration added, removed or moved | `AGENT-CONFIG.md` |

`BACKLOG.md` is a dated snapshot. Refresh it from the tracker rather than editing it by hand, and
trust the tracker when the two disagree.

A full refresh — re-running the investigation and re-verifying every document — is done with the
organisation's `repo-knowledge-base` agent skill.
