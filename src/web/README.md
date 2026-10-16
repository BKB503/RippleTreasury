# Ripple Treasury React UI

The API build automatically installs locked dependencies when needed, builds this React/Vite app, and copies dist into API output/publish wwwroot. F5 and the published API serve the compiled UI at http://localhost:5080/ and Swagger at /swagger. Node is required only when building from source.

Optional frontend development: npm ci; npm run dev. Vite listens on 5173 and proxies /api to 5080. No Vite server is needed for normal F5 or browser tests.

Checks: npm run build; npm run lint; npx playwright install chromium; npm run test:e2e. Playwright starts one API with an isolated database and exercises the production bundle. Stop another API on port 5080 first. See the root [README](../../README.md).
