# MailDev and MailKit custom integrations

This sample demonstrates how to build custom .NET Aspire hosting and client integrations with Aspire 13.6. The runnable AppHost is written in TypeScript and consumes the C# hosting integration through the Aspire Type System (ATS).

## Projects

- `MailDev.Hosting` models a MailDev container, its web and SMTP endpoints, and a deferred connection string.
- `MailKit.Client` registers a scoped MailKit SMTP client, health checks, tracing, and metrics in a consuming service.
- `NewsletterService` sends subscription and unsubscription messages through the integrations.
- `ServiceDefaults` configures standard Aspire health checks and OpenTelemetry.
- `CSharpAppHost` is a compile-validated C# equivalent of the runnable TypeScript AppHost.

## Aspire Type System

The hosting integration marks `MailDevResource` and `AddMailDev` with `[AspireExport]`. The local project reference in `aspire.config.json` lets `aspire restore` inspect those exports and generate the TypeScript `addMailDev` API under `.aspire/modules`.

Generated files under `.aspire/modules` are not source files and must not be edited or committed.

```json
"packages": {
  "MailDev.Hosting": "MailDev.Hosting/MailDev.Hosting.csproj"
}
```

The TypeScript AppHost uses the generated API:

```typescript
const maildev = await builder.addMailDev("maildev");

await builder.addCSharpApp("newsletterservice", "./NewsletterService")
    .withHttpHealthCheck({ path: "/health" })
    .withExternalHttpEndpoints()
    .withReference(maildev)
    .waitFor(maildev);
```

The equivalent C# AppHost code is:

```csharp
var maildev = builder.AddMailDev("maildev");

builder.AddProject("newsletterservice", "../NewsletterService/NewsletterService.csproj")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(maildev)
    .WaitFor(maildev);
```

## Secure credentials

`AddMailDev` creates a secret password parameter by default. The password is passed to MailDev through `MAILDEV_INCOMING_PASS` and to the newsletter service through a deferred connection string:

```text
Endpoint=smtp://{maildev.bindings.smtp.host}:{maildev.bindings.smtp.port};Username=mail-dev;Password={maildev-password.value}
```

The AppHost model retains parameter and endpoint references instead of embedding a password or allocated port in source code. MailKit parses the resolved connection string, connects to SMTP, and authenticates for each service scope.

## Run the sample

Prerequisites are .NET 10, Node.js 24 or a supported Node.js 20/22 release, Docker, and the [Aspire 13.6 prerelease CLI used by this repository](../../README.md#aspire-version).

```powershell
aspire restore
npm install
npm run aspire:build
aspire run
```

Use the newsletter service endpoint shown in the Aspire dashboard:

```http
POST /subscribe
Content-Type: application/json

{ "email": "reader@example.com" }
```

Open the MailDev web endpoint from the dashboard to inspect the generated message. Use `POST /unsubscribe` with the same payload to send the unsubscription message.

## Tests

```powershell
dotnet test MailDev.Hosting.Tests/MailDev.Hosting.Tests.csproj
dotnet test MailKit.Client.Tests/MailKit.Client.Tests.csproj
npm run aspire:build
npm run aspire:lint
dotnet build CSharpAppHost/CSharpAppHost.csproj
```