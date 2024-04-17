using AspireTurboMonoRepo.AppHost;

//
// NOTE: The .csproj file for the AspireTurboMonoRepo.AppHost project declares
//       a two-step pre-build action. The first step checks for the existence
//       of a top-level node_modules file in ../kitchen-sink/ (or any folder
//       matching the ../*/ pattern containing a package.json file), and,
//       if it does not exist, runs pnpm install to create and populate the
//       node_modules folder(s). The second step runs a top level pnpm build,
//       which runs turbo build. Turbo build automatically caches build outputs,
//       so subsequent builds of the node apps are generally quite fast.
//

var builder = DistributedApplication.CreateBuilder(args);

//
// The default kitchen-sink/apps/api does not return a value for the 
// default ("/") endpoint path. To test that the api app is working,
// visit the "/status" endpoint, which should return { "ok": true }
//
// api: npm run start
builder.AddGenericNodeApp("api", "../kitchen-sink/apps/api", "pnpm","start")
    .WithHttpEndpoint(targetPort: 3000, env: "PORT");

//
// The default kitchen-sink/apps/admin project required two manual edits
// to allow Aspire to control the port via the PORT environment variable.
// First, in the package.json file, the start script needed to be updated
// to NOT hardcode the port number. Second. the vite.config.ts file needed
// to be updated to use the PORT environment variable to specify the port.
//
// admin: npm run dev (use the vite dev server to host the app)
builder.AddGenericNodeApp("admin", "../kitchen-sink/apps/admin", "pnpm", "dev")
    .WithHttpEndpoint(targetPort: 3001, env: "PORT");

// blog: npm run start (use the remix dev server to host the app)
builder.AddGenericNodeApp("blog", "../kitchen-sink/apps/blog", "pnpm", "dev")
    .WithHttpEndpoint(targetPort: 3002, env: "PORT");

// storefront: npm run start
builder.AddGenericNodeApp("storefront", "../kitchen-sink/apps/storefront", "pnpm", "start")
    .WithHttpEndpoint(targetPort: 3003, env: "PORT");

builder.Build().Run();
