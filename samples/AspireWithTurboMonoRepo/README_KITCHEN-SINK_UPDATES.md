## Note when updating kitchen-sink versions

This Aspire sample app deploys a turbo mono repo example based on the official
[Vercel Turbo Kitchen-Sink example app](https://vercel.com/templates/remix/turborepo-kitchensink)

If you are updating this Aspire sample to use a newer copy of the
Vercel Turbo Kitchen-Sink example app, as of this writing you will likely need to make  a couple of manual edits to the Vercel example code:

First, in the `kitchen-sink/package.json` file, four additional scripts needs to be added to allow Aspire to build-and-run the apps individually (as opposed to doing an overall build of all the apps, as is normally done with turbo builds):

    "build-and-start-api": "turbo run build --filter=api && turbo run start --filter=api",
    "build-and-dev-admin": "turbo run build --filter=admin && turbo run dev --filter=admin",
    "build-and-dev-blog": "turbo run build --filter=blog && turbo run dev --filter=blog",
    "build-and-start-storefront": "turbo run build --filter=storefront && turbo run start --filter=storefront"


Second, in the `kitchen-sink/apps/admin/package.json` file, the start script needs to be edited to NOT hardcode the port number in the vite command line:

```
"dev_ORIGINAL": "vite --host 0.0.0.0 --port 3001 --clearScreen false",
"dev": "vite --host 0.0.0.0 --clearScreen false",
```

Third, in the `kitchen-sink/apps/admin/vite.config.ts` file, the export needs to be modified to use the PORT environment variable to specify the port:

```
export default defineConfig({
  plugins: [react()],
  //
  // BEGIN added to support Aspire hosting
  //
  server: {
    port: Number(process.env.PORT ?? 3001),
  },
  preview: {
    port: Number(process.env.PORT ?? 3001),
  },
  //
  // END added to support Aspire hosting
  //
});
```

Fourth, the `kitchen-sink` folder needs to receive a copy of [the license file found at the root of the vercel turbo repo](https://github.com/vercel/turbo/blob/main/LICENSE).
