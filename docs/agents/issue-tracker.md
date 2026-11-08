# Local Markdown issue tracker

Use one directory per feature at `.scratch/<feature-slug>/`. Keep the spec in
`spec.md` and give each implementation issue its own file under
`issues/<NN>-<slug>.md`, starting at `01`.

Record triage state in a `Status:` line near the top of each issue. Use the
values in `triage-labels.md`. Append discussion under `## Comments`.

To publish a spec or issue, create its file and any needed parent directories.
To fetch an issue, read the file identified by its path or by its number
within the relevant feature.

## Wayfinding

- Store each effort's Notes, Decisions so far, and Fog in `.scratch/<effort>/map.md`.
- Store each child ticket in `issues/<NN>-<slug>.md` under the effort directory.
  Put the question in the body and set `Type:` to `research`, `prototype`,
  `grilling`, or `task`. Keep `Status:` for the five triage labels; track
  implementation lifecycle separately in `Execution:`.
- List prerequisites near the top as `Blocked by: NN, NN`. The ticket becomes
  unblocked when every listed ticket has `Execution: resolved`.
- Take the lowest-numbered open, unblocked, unclaimed ticket. Save
  `Execution: claimed` before starting work.
- To resolve a ticket, append the result under `## Answer`, set
  `Execution: resolved` and `Status: ready-for-human`, and add a short summary
  and link to Decisions so far.
