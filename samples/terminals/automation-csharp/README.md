# Terminal automation: C# AppHost

Drive a real REST TUI from AppHost code. **Run terminal walkthrough** presses keys in Slumber, waits for its responses, and independently checks the API after creating, reading, updating, and deleting a note.

![Completion popup after the verified Slumber walkthrough](./images/automation-result.png)

## Prerequisites and startup

- Aspire CLI **13.6.0**, installed using the [root installation instructions](../../../README.md#aspire-version).
- The .NET 10 SDK selected by [`samples/global.json`](../../global.json).
- A running Linux container runtime.

See the collection's [Aspire version requirements](../README.md#aspire-version). Keep the sibling [`basics-csharp`](../basics-csharp/) directory: this sample reuses its API, ServiceDefaults, and Slumber Dockerfile rather than duplicating them. It launches its **own** API, Redis, and Slumber resources; the basic AppHost does not need to be running.

From this directory:

```sh
aspire restore --apphost apphost.cs
aspire start --apphost apphost.cs --isolated
aspire wait --apphost apphost.cs
```

Open Slumber's **Console logs** in the dashboard. Click the highlighted **Play** button beside **Resource actions** to run the walkthrough. An interaction-service confirmation explains that Slumber must restart to put its terminal in a known state and that the reserved `automation` note will be reset. Choose **Restart and run** to continue, or **Cancel** to leave the terminal and data unchanged. Do not type into Slumber or resize its terminal during the walkthrough.

Use the highlighted button for repeat runs. If the console page's overflow menu retains a stale disabled command after completion, use the highlighted toolbar button or reload the page to refresh the command state.

Alternatively, run the same command without a browser:

```sh
aspire resource slumber walkthrough --apphost apphost.cs --non-interactive
```

This explicit non-interactive invocation skips the confirmation and logs that it will restart Slumber and reset the reserved note. It retains the original headless automation behavior.

Success leaves **04 - Delete automation note** selected with **204 No Content** and shows a **Terminal walkthrough complete** popup summarizing the verified CRUD round trip. Choose **Done** to return to Slumber and run again. No completion popup is shown for canceled or failed automation, and non-interactive invocations report their result through the CLI instead.

The command logs each verified step to the Slumber resource and fails if a screen or API assertion fails. View those messages with `aspire logs slumber --apphost apphost.cs` (the output also contains the TUI's terminal escape sequences).

## What the sample demonstrates

[`apphost.cs`](./apphost.cs) acquires the resource-owned terminal with `TerminalService.TryGetTerminal("resource:slumber:0", ...)`. The final `0` identifies the only replica. It uses `SendKeyAsync` for navigation and `WaitForTextAsync` for actual rendered output; it does not use sleeps as evidence of request completion.

Each run:

1. Acquires a non-blocking semaphore so two walkthroughs cannot type simultaneously.
2. Uses `IInteractionService.PromptConfirmationAsync` when interactions are available. Cancellation returns without changing resources or data. After confirmation, it waits for the API to be healthy and removes only the reserved `automation` note through HTTP.
3. Restarts only Slumber and waits for a new running container ID, resetting its selection and in-memory history.
4. Waits for each recipe's empty history, sends the request, and waits for its expected status. It also checks response text and verifies API state; deletion must produce an independent HTTP 404.
5. Releases the terminal handle without stopping Slumber, leaving the final response visible, and displays the completion message when interactions are available.

The [four recipes](./requests.yml) use `persist: false`, so saved responses cannot satisfy the next run's assertions. The request collection is mounted **read-only**; edit the repository file, not Vim inside Slumber. API endpoint injection still comes from Aspire, with no hard-coded host port.

The command honors cancellation and has a two-minute automation deadline, starting after confirmation, in addition to the terminal API's per-wait timeout. Time spent deciding at the confirmation prompt or dismissing the completion popup does not consume that deadline. Failures are logged and returned as failed resource commands. A failed run may leave the `automation` note behind; the next run resets it. No other notes are deleted.

This is deliberately a version-specific TUI automation example: changing the collection order, Slumber version, or screen layout can require updating its assertions. Human input and other clients editing the reserved note during a run are not supported. Normal manual use is available again once the command finishes.

## Repeatability and cleanup

Run the command twice to exercise both initial and repeated execution. No browser needs to be attached. The API reused here has its own focused tests:

```sh
dotnet test ../basics-csharp/tests/Notes.Tests.csproj
aspire stop --apphost apphost.cs
```

This sample uses an unauthenticated HTTP API and disposable Redis data. Keep its endpoints and dashboard private to trusted users; the Redis REPL grants authenticated database access. Use only non-sensitive demo notes. Stopping this AppHost does not stop a separately running basic sample.
