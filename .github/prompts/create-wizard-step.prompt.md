---
description: "Create and wire a new BackupHelper wizard step"
agent: Ask
---
Use the .NET Wizard Step Specialist agent.

Task:
Add a new wizard step in BackupHelper.ConsoleApp and wire it into the existing flow.

Inputs (fill these before running):
- Step name: <StepName>
- Purpose: <What this step should do>
- Entry step(s): <Which step(s) should navigate here>
- Success next step: <Next step on success>
- Retry behavior: <How invalid input is handled>
- Cancel/back behavior: <Where to return on cancel>
- Required API operations: <MediatR commands/queries to await>
- Sensitive inputs: <None or list of secrets/passwords>
- Target files (optional): <Paths>

Requirements:
1. Follow wizard contracts:
   - <StepName>StepParameters implements IWizardParameters.
   - <StepName>Step implements IWizardStep<<StepName>StepParameters>.
   - Handle remains async and returns the next IWizardParameters instance or null.
2. Keep prompt/input/branching logic in ConsoleApp wizard layer.
3. Keep business logic in Api/Core via awaited mediator calls.
4. Handle SensitiveString ownership and disposal correctly.
5. Do not log or print secrets.
6. Keep changes minimal and consistent with existing naming and flow patterns.
7. If dependencies change, ensure DI resolution remains valid.
8. Pass cancellationToken through mediator operations.
9. Update only required files.

Process:
1. Provide a brief plan and transition graph (before -> after).
2. Implement the changes.
3. Run targeted build/tests where feasible.
4. Summarize changed files and updated flow behavior.

Output format:
- Plan
- Changes made
- Validation results
- Follow-up risks or next actions
