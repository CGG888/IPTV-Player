import fs from 'node:fs';
import path from 'node:path';

const projectRoot = process.cwd();
const vueIndexMjsPath = path.join(projectRoot, 'node_modules', 'vue', 'index.mjs');

if (!fs.existsSync(vueIndexMjsPath)) {
  process.exit(0);
}

const desired = "export * from './dist/vue.esm-bundler.js'\n";
const current = fs.readFileSync(vueIndexMjsPath, 'utf8');

if (current !== desired) {
  fs.writeFileSync(vueIndexMjsPath, desired, 'utf8');
}
