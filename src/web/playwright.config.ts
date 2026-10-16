import { defineConfig } from "@playwright/test";
import path from "node:path";
import os from "node:os";
const database = path.join(os.tmpdir(), `ripple-e2e-${Date.now()}.db`);
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  workers: 1,
  use: { baseURL: "http://localhost:5080", trace: "retain-on-failure" },
  webServer: [
    {
      command:
        "dotnet run --project ../RippleTreasury.Api --no-launch-profile --urls http://localhost:5080",
      url: "http://localhost:5080/health/ready",
      reuseExistingServer: false,
      timeout: 300000,
      env: {
        ASPNETCORE_ENVIRONMENT: "Development",
        Frontend__OpenBrowser: "false",
        Database__Path: database,
        Processing__EmbeddedWorker: "true",
      },
    },
  ],
});
