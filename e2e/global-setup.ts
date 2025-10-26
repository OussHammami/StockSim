import { exec as execCb } from 'node:child_process';
import { promisify } from 'node:util';
import waitOn from 'wait-on';

const exec = promisify(execCb);

async function waitForServices(timeoutMs: number) {
  await waitOn({
    resources: [
      'http-get://localhost:8080/readyz',
      'http-get://localhost:8081/healthz',
      'http-get://localhost:5173'
    ],
    timeout: timeoutMs,
    interval: 1000,
    simultaneous: 3,
    validateStatus: s => s >= 200 && s < 500
  });
}

async function ensureComposeUp() {
  const cmd = 'docker compose -p e2e up -d --build postgres rabbitmq marketfeed web react';
  await exec(cmd, { env: process.env });
}

export default async function globalSetup() {
  try {
    // Fast path: services already up
    await waitForServices(15_000);
    return;
  } catch {
    // Start compose and wait longer
  }
  try {
    await ensureComposeUp();
    await waitForServices(120_000);
  } catch (err) {
    console.error('Failed to start/wait for Docker Compose stack:', err);
    throw err;
  }
}