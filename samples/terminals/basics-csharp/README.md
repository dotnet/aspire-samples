# Terminal basics: C# AppHost and API

Run [Slumber](https://github.com/LucasPickering/slumber), an interactive REST client, inside the Aspire dashboard. Create a note through an ASP.NET Core API, edit it directly in Redis's authenticated REPL, then retrieve the changed value in Slumber.

![Slumber displaying a note edited through the Redis REPL](./images/slumber-roundtrip.png)

## Prerequisites and startup

- Aspire CLI **13.6.0**, installed using the [root installation instructions](../../../README.md#aspire-version).
- The .NET 10 SDK selected by [`samples/global.json`](../../global.json).
- Docker or another supported container runtime, running with Linux containers.

See the collection's [Aspire version requirements](../README.md#aspire-version).

From this directory:

```sh
aspire restore --apphost apphost.cs
aspire start --apphost apphost.cs --isolated
aspire wait --apphost apphost.cs
```

Open the dashboard URL printed by Aspire. Wait for `redis` and `api` to be healthy and `slumber` to be running. The first start builds the Slumber image. Ports and the container-reachable API address are supplied by Aspire; do not enter a URL into Slumber.

The non-root container packages checksum-verified Slumber **5.3.0** binaries for Linux ARM64/AMD64 and includes Vim. No host Slumber or Vim installation is needed.

## What the AppHost adds

[`apphost.cs`](./apphost.cs) declares three resources:

```csharp
var redis = builder.AddRedis("redis").WithRepl();

var api = builder.AddProject("api", "./api/Notes.Api.csproj")
    .WithReference(redis)
    .WithHttpHealthCheck("/health")
    .WaitFor(redis);

builder.AddDockerfile("slumber", "./slumber")
    .WithEnvironment("BASE_URL", api.GetEndpoint("http"))
    .WaitFor(api)
    .WithTerminal();
```

`WithTerminal()` attaches to Slumber's foreground process. Redis's `WithRepl()` instead adds a resource action that starts a separate, already authenticated `redis-cli`. The AppHost suppresses the terminal APIs' experimental diagnostic locally.

The API uses `Aspire.StackExchange.Redis` and sample-local ServiceDefaults for health checks and telemetry. Slumber receives only the API endpoint, not Redis credentials. Its [saved requests](./slumber/slumber.yml) read that endpoint with `{{ env('BASE_URL') }}`. HTTP is intentional for this local container-to-API connection; no certificate validation is disabled.

The optional `http` profile in [`apphost.run.json`](./apphost.run.json) also uses unencrypted local dashboard, telemetry, and resource-service connections. This is separate from the API's HTTP traffic and does not disable dashboard authentication. Keep these loopback endpoints private; do not expose or forward this profile to untrusted networks.

## Try the round trip

1. Open **Console logs** for `slumber`. Focus the terminal and press **r** to focus the request list. Use **Up/Down** to select **01 - Create a note**, then **Enter** to send. Expect **201 Created** (or **200 OK** if `welcome` already exists).
2. Select **02 - List notes** or **03 - Read welcome** and press **Enter**. The JSON includes `"text": "Hello from Slumber!"`.
3. On the Resources page, open `redis`'s **Actions** menu and select **REPL**. No password entry is needed. Run:

   ```text
   HGETALL notes
   HSET notes welcome "Edited from Redis"
   HGET notes welcome
   ```

4. Hide the terminal panel and return to Slumber. Select **03 - Read welcome** and press **Enter**. Expect **200 OK** and `"text": "Edited from Redis"` without restarting the API.
5. Send **04 - Update welcome** for `"text": "Updated from Slumber!"`, then **05 - Delete welcome** for **204 No Content**. Reading the deleted note returns **404**. **06 - API and Redis health** checks readiness.

**Tab/Shift+Tab** moves between panes, **r** returns to the request list, and **?** shows help. Selecting a request loads it immediately; **Enter** sends it.

Vim is configured as the default editor. In Vim, press **i** to edit, then **Esc** and **:wq** to save and return to Slumber (**:q!** discards changes). Collection edits inside the container are disposable; edit `slumber/slumber.yml` in the repository to retain them across rebuilds.

## Data and lifecycle

The API stores plain text in the Redis hash `notes`, with note IDs as fields. IDs accept 1-40 lowercase ASCII letters, digits, or hyphens. A JSON `text` value must contain 1-1000 UTF-16 characters and cannot be whitespace-only. PUT creates (201) or replaces (200); missing reads/deletes return 404, invalid input returns 400, and Redis failures return 503. Request bodies are limited to 10 KiB.

There is no persistent volume or API-local cache. To reset only this demo's data, run `DEL notes` in the REPL, then resend the create request. Do not use `FLUSHALL`.

Type `quit` in `redis-cli` before closing its tab. Hiding a terminal panel is not a process stop. Slumber exits with **q** (or **Ctrl+C**); use its resource **Start** or **Restart** action to launch it again. Reloading the dashboard reconnects to a still-running Slumber process. Use **Fit** or the terminal size selector to adjust its layout.

Stop this sample explicitly:

```sh
aspire stop --apphost apphost.cs
```

## Checks and troubleshooting

```sh
dotnet test tests/Notes.Tests.csproj
```

The focused tests cover the API contract with an in-memory store; the walkthrough exercises real Redis and the interactive terminals. `TerminalNotes.slnx` includes the API, ServiceDefaults, and tests in the repository build.

If **REPL** is missing, check `aspire --version` and restart with the matching CLI. If Slumber is waiting, inspect `api` and `redis` health first. If requests cannot connect, verify that `BASE_URL` is populated by the AppHost; do not replace it with `localhost` (which means the Slumber container itself). Changes to the Dockerfile or saved collection require rebuilding/restarting the sample.

Keep the dashboard private to trusted users: its Redis REPL grants authenticated datastore access. The unauthenticated HTTP notes API allows anyone who can reach it to read, replace, and delete every demo note. Use only disposable, non-sensitive data; this is a local demonstration, not production.
