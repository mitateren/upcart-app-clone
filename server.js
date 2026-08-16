/**
 * Plesk Node.js Application startup file.
 * Plesk → Domain → Node.js → Application startup file: server.js
 */
import "dotenv/config";
import { spawn } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.dirname(fileURLToPath(import.meta.url));
const port = process.env.PORT || process.env.OPENSHIFT_NODEJS_PORT || "3000";

const cli = path.join(root, "node_modules", "@react-router", "serve", "dist", "cli.js");
const serverBuild = path.join(root, "build", "server", "index.js");

const child = spawn(process.execPath, [cli, serverBuild], {
  cwd: root,
  stdio: "inherit",
  env: {
    ...process.env,
    PORT: String(port),
    NODE_ENV: process.env.NODE_ENV || "production",
  },
});

child.on("exit", (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }
  process.exit(code ?? 1);
});
