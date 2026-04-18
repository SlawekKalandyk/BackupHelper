---
description: "Edit an existing BackupHelper wizard flow safely"
agent: Ask
---
Use the .NET Wizard Step Specialist agent.

Task:
Modify an existing wizard flow in BackupHelper.ConsoleApp. This may include inserting, removing, reordering, splitting, or merging wizard-step paths.

Inputs (fill these before running):
- Flow area: <Main menu / create backup / backup profiles / credential profiles / other>
- Change type: <insert | remove | reorder | split | merge>
- Affected step(s): <List step classes and parameter types>
- New transition map: <FromStep -> ToStep paths>
- Validation/retry behavior: <How invalid input is handled>
- Cancel/back behavior: <Where flow returns on cancel/back>
- Required API operations: <MediatR commands/queries to await>
- Sensitive inputs: <None or list of secrets/passwords>
- Compatibility constraints: <Behavior that must remain unchanged>
- Target files (optional): <Paths>

Requirements:
1. Preserve wizard contracts:
   - Step parameters implement IWizardParameters.
   - Step handlers implement IWizardStep<TParameters>.
   - Transitions return IWizardParameters or null intentionally.
2. Update both inbound and outbound transitions for every affected step.
3. Keep prompt/input/branching logic in ConsoleApp wizard layer.
4. Keep business logic in Api/Core via awaited mediator calls.
5. Pass cancellationToken through all mediator operations.
6. Handle SensitiveString ownership and disposal correctly.
7. Do not log or print secrets.
8. Keep default credential provider setup/cleanup balanced across changed paths.
9. Keep changes minimal and consistent with existing naming and navigation patterns.

Safety checks:
- No dead-end step unless it is intentionally terminal.
- No unreachable step introduced by transition edits.
- Main loop compatibility remains intact (Program awaits mediator.Send until null).
- Error and cancel paths return to expected menu/entry steps.

Process:
1. Map the current flow graph for affected steps.
2. Propose the updated flow graph and behavior deltas.
3. Implement the minimal required changes.
4. Run targeted build/tests where feasible.
5. Summarize transition updates and residual risks.

Output format:
- Current flow summary
- Proposed flow changes
- Changes made
- Validation results
- Residual risks and follow-ups
