#!/usr/bin/env node
const port = process.env['PORT'] || 4200;
const { spawn } = require('child_process');

const proc = spawn(
  'npx',
  ['ng', 'serve', '--port', String(port), '--host', '0.0.0.0'],
  { stdio: 'inherit', shell: true }
);

proc.on('exit', code => process.exit(code ?? 0));
