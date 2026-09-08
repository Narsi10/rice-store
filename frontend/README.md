# Rice Store - Angular Frontend

Node.js was not installed on the build machine, so the Angular workspace
shell was not generated here. Generate it once Node 20+ is installed:

```bash
npm install -g @angular/cli
ng new rice-store-web --standalone --routing --style=scss
cd rice-store-web
```

Then drop the files under `src/` (in this folder) into the generated project.
They are written against Angular standalone components + HttpClient and talk to
the API Gateway at `http://localhost:8000`.

## Environment

All calls go through the single gateway origin. Set it in
`src/environments/environment.ts`:

```ts
export const environment = {
  production: false,
  gatewayUrl: 'http://localhost:8000'
};
```

## What the sample files show

- `core/api.service.ts`   - typed HTTP client for every backend endpoint
- `catalog/products.component.ts` - lists rice products from the Catalog service
- `models.ts`             - shared TypeScript models mirroring the C# DTOs

The frontend never calls a service directly. Every request hits the gateway,
which routes `/api/products/*` to Catalog, `/api/cart/*` to Cart, and so on.
