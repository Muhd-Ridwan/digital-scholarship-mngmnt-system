# DIGITAL SCHOLARSHIP MANAGEMENT SYSTEM

## TECH STACK

1. Frontend: Angular + Tailwind CSS
2. Backend: C#
3. AWS Service:
   `RDS: For relational database`
   `DynamoDB: For storing audit log purposes`
   `Cognito: For issuing JWT user login using RBAC`
   `AWS EB: To deploy backend`
   `AWS Amplify /  S3 + Cloudfront: For frontend deployment`<br/>
   It is a group assignment consist of 4 team members under module name Designing &amp; Developing Cloud Applications

---

## Frontend Setup Guide - Angular (ClientApp)

## 1. Prerequisites

Install these before doing anything else (If you havent install it):

1. **Node.js (LTS version)** — download from [nodejs.org](https://nodejs.org). This includes `npm`.
2. Angular CLI\*\* (global install):

```powershell
   npm install -g @angular/cli
```

Verify Installation

```powershell
   ng version
```

3. Recommended editor for Frontend is using Visual Studio Code.

---

## 2. Cloning and installing dependencies

```powershell
git clone <repo-url>
cd digital-scholarship-mngmnt-system/Digital-Scholarship-Management-System/ClientApp
npm install
```

`npm install` reads `package.json` and installs all dependencies (Angular, Tailwind, etc.) into a local `node_modules` folder — this folder is gitignored and regenerated locally, not pulled from Git.

---

## 3. Running the frontend locally

```powershell
ng serve
```

(or `npm start`, same thing)

This starts the Angular dev server at `http://localhost:4200` with live-reload — changes to your code auto-refresh the browser.

**Important — the backend must also be running** for the app to actually work (API calls will fail otherwise):

- Open the backend project (`Digital-Scholarship-Management-System.API`) in **Visual Studio**
- Run it using the **`https`** launch profile specifically (not `http`) — must match the port configured in `environment.ts`
- Backend runs at `https://localhost:7192`
  Both processes (`ng serve` in one terminal/VS Code, backend in Visual Studio) run **simultaneously**, in separate windows.

---

## 4. Project structure

```
ClientApp/
└── src/
    └── app/
        ├── admin/              → Superadmin role pages/components
        │   ├── components/
        │   └── pages/
        ├── core/                → App-wide shared logic
        │   ├── guards/           → route guards (role-based access)
        │   ├── interceptors/     → HTTP interceptors (attaches JWT to requests)
        │   └── services/         → AuthService, ApiService, etc.
        ├── officer/             → HR/Admin (Officer) role pages/components
        │   ├── components/
        │   └── pages/
        ├── shared/
        │   └── components/       → reusable components used across roles
        ├── sponsor/             → Sponsor Provider role pages/components
        │   ├── components/
        │   └── pages/
        ├── student/             → Student role pages/components
        │   ├── components/
        │   └── pages/
        ├── environments/
        │   ├── environment.ts        → local dev config
        │   └── environment.prod.ts   → production build config
        ├── app.config.ts        → app-wide providers (router, HttpClient, etc.)
        ├── app.routes.ts        → route definitions
        └── app.ts               → root component
```

## 5. Recommended VS Code Extensions

Install these in VS Code (Extensions panel, `Ctrl+Shift+X`):

| Extension                                   | Why                                                                  |
| ------------------------------------------- | -------------------------------------------------------------------- |
| **Angular Language Service** (Angular team) | IntelliSense inside HTML templates, error checking, go-to-definition |
| **Tailwind CSS IntelliSense**               | Autocomplete + hover preview for Tailwind utility classes            |
| **Prettier - Code formatter**               | Auto-formats code on save, keeps style consistent across the team    |
| **ESLint**                                  | Catches code quality issues                                          |
| **Path Intellisense**                       | Autocompletes file paths in import statements                        |
| **GitLens**                                 | Inline Git blame, useful for a team project                          |

Why VS Code and not Visual Studio for the frontend: Visual Studio's Solution Explorer only tracks `.sln`/`.csproj`-registered projects — Angular is a separate Node.js-based project with its own tooling, so it's opened as a plain folder in VS Code instead and not a workaround.

---

## 6. Environment Configuration

Angular does **not** use `.env` files — it's a client-side SPA compiled into static files at build time, with no running server process to read runtime environment variables. Configuration is baked into the build instead, via two files:

### `src/environments/environment.ts` (local development)

```typescript
export const environment = {
  production: false,
  apiUrl: "https://localhost:7192/api",
};
```

> Port `7192` must match the API's **https** launch profile. If you get connection-refused errors, check this first — it's usually a port mismatch, not a code bug.

### `src/environments/environment.prod.ts` (production build)

```typescript
export const environment = {
  production: true,
  apiUrl: "/api",
};
```

`angular.json` is configured with `fileReplacements` so `ng build` automatically swaps `environment.ts` → `environment.prod.ts`. You don't need to change any import paths — components always import from `environment.ts`, and the build process swaps the file content behind the scenes.

**Do not gitignore these files** — they contain no secrets (just a URL), and Angular's compiled output is publicly visible in the browser regardless. Never put real secrets (API keys, credentials) in Angular — those belong only in the backend.

---

## 7. Available commands

| Command                        | What it does                                            | When to use                 |
| ------------------------------ | ------------------------------------------------------- | --------------------------- |
| `ng serve` (or `npm start`)    | Dev server, live-reload, uses `environment.ts`          | Daily development           |
| `ng build`                     | Production build, optimized, uses `environment.prod.ts` | Before deployment           |
| `ng generate component <path>` | Scaffolds a new component                               | When creating new UI pieces |

---

## 8. Tailwind CSS

Tailwind is already installed and configured (`.postcssrc.json`, `@import "tailwindcss";` in `styles.css`). Just use utility classes directly in your component templates:

```html
<div class="flex items-center gap-4 p-6 bg-white rounded-lg shadow-md">...</div>
```

## If you're building a data-heavy screen (tables, filters — e.g. HR's application review list, Superadmin's audit log viewer), consider pairing Tailwind with **Angular Material** or **PrimeNG** rather than building tables from scratch

## 9. Setup prompts reference (for reference only — do NOT re-run `ng new`)

If anyone ever needs to scaffold a _new_ Angular project from scratch (not this one), here's what was selected when this project was created:

| Prompt                                             | Answer   | Why                                                                                                                                                                          |
| -------------------------------------------------- | -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Share pseudonymous usage data with Angular/Google? | **N**    | No functional impact, just skips telemetry                                                                                                                                   |
| Enable Server-Side Rendering (SSR) and SSG?        | **N**    | This is a pure client-side SPA, served as static files from the backend — SSR would require a separate Node.js server, conflicting with the one-compute-service architecture |
| Configure AI tools with Angular best practices?    | **None** | Not functionally needed; keeps repo free of AI-tooling config given the module's Yellow AI usage policy                                                                      |

Project was created with:

```powershell
ng new ClientApp --routing --style=css
```

---

## 10. Common issues

| Problem                             | Likely cause                       | Fix                                                                                                               |
| ----------------------------------- | ---------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `ng: command not found`             | Angular CLI not installed globally | `npm install -g @angular/cli`                                                                                     |
| API calls fail / connection refused | Backend not running, or wrong port | Confirm backend is running on the **https** profile, confirm port matches `environment.ts`                        |
| CORS error in browser console       | Origin mismatch                    | Confirm `http://localhost:4200` is listed in backend's `appsettings.Development.json` under `Cors:AllowedOrigins` |
| Styles not applying                 | Tailwind misconfigured             | Check `.postcssrc.json` exists and `styles.css` has `@import "tailwindcss";` at the top                           |
