import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const script = fileURLToPath(import.meta.url);
const web = path.resolve(path.dirname(script), "../src/web");
const hash = (value) => createHash("sha256").update(value).digest("hex");
function npm(args) {
  const result = process.platform === "win32"
    ? spawnSync("cmd.exe", ["/d", "/s", "/c", `npm.cmd ${args.join(" ")}`], { cwd: web, stdio: "inherit" })
    : spawnSync("npm", args, { cwd: web, stdio: "inherit" });
  if (result.error || result.status !== 0) {
    throw new Error(`npm ${args.join(" ")} failed. Install Node.js 22 LTS/npm and check the build output.`, { cause: result.error });
  }
}
const dependencyHash = hash(readFileSync(path.join(web, "package-lock.json")));
const dependencyStamp = path.join(web, "node_modules/.ripple-dependencies");
if (!existsSync(dependencyStamp) || readFileSync(dependencyStamp, "utf8") !== dependencyHash ||
    !existsSync(path.join(web, "node_modules/vite/package.json"))) {
  console.log("Installing locked frontend dependencies (npm ci)...");
  npm(["ci", "--no-audit", "--no-fund"]);
  writeFileSync(dependencyStamp, dependencyHash);
}
function files(folder) {
  if (!existsSync(folder)) return [];
  return readdirSync(folder, { withFileTypes: true }).sort((a, b) => a.name.localeCompare(b.name))
    .flatMap((item) => item.isDirectory() ? files(path.join(folder, item.name)) : [path.join(folder, item.name)]);
}
const inputs = [script, ...files(path.join(web, "src")), ...files(path.join(web, "public")),
  ...readdirSync(web).filter((name) => /^(package.*\.json|tsconfig.*\.json|vite\.config\..*|index\.html|\.env.*)$/.test(name)).sort().map((name) => path.join(web, name))];
const inputHash = hash(process.version + inputs.map((file) => path.relative(web, file) + hash(readFileSync(file))).join("\n"));
const stateFile = path.join(web, "obj/frontend-build.json");
let previous;
try { previous = JSON.parse(readFileSync(stateFile, "utf8")); } catch { /* First build or stale cache. */ }
if (previous?.inputHash === inputHash && previous.outputs?.length &&
    previous.outputs.every((file) => existsSync(path.join(web, "dist", file)))) {
  console.log("React bundle is current; reusing compiled assets.");
} else {
  npm(["run", "build"]);
  if (!existsSync(path.join(web, "dist/index.html"))) throw new Error("React build did not produce dist/index.html.");
  mkdirSync(path.dirname(stateFile), { recursive: true });
  writeFileSync(stateFile, JSON.stringify({ inputHash, outputs: files(path.join(web, "dist")).map((file) => path.relative(path.join(web, "dist"), file)) }));
}
