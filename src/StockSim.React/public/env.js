// Runtime configuration injected by the hosting environment (Docker/K8s/cloud).
// Defaults are safe for local dev.
window.__env = window.__env || {
  VITE_MARKETFEED_URL: "http://localhost:8081"
};
