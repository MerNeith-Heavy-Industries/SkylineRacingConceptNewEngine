// ── NFMW JS ↔ C# Bridge ──────────────────────────────────────────
//
// On page load, NfmwLoadHandler injects:
//   - window.nfmwEvents  — event emitter for C# → JS pushes
//   - window.__nfmwDispatch(event, data) — dispatch hook for C# pushes
//
// The V8 context also exposes:
//   - window.nfmw.call(methodName, jsonPayload) — JS → C# calls
//
// This module provides typed wrappers.

/** Typed listener for C# → JS events. */
type NfmwListener<T = unknown> = (data: T) => void;

/** The nfmwEvents emitter injected by CEF on page load. */
interface NfmwEvents {
  _listeners: Record<string, [listener: NfmwListener, converter: ((buffer: ArrayBuffer) => unknown) | undefined][]>;
  on<T = unknown>(event: string, callback: NfmwListener<T>, converter?: (buffer: ArrayBuffer) => T): void;
  off<T = unknown>(event: string, callback: NfmwListener<T>): void;
  emit<T = unknown>(event: string, data: T): void;
}

declare global {
  interface Window {
    nfmwEvents: NfmwEvents;
    __nfmwDispatch: (event: string, data: unknown) => void;
    __nfmwCall: (methodName: string, ...args: unknown[]) => void;
  }
}

const listeners: Record<string, [listener: NfmwListener, converter: ((buffer: ArrayBuffer) => unknown) | undefined][]> = {};
window.nfmwEvents = {
  _listeners: listeners,
  on<T = unknown>(event: string, cb: NfmwListener<T>, converter?: (buffer: ArrayBuffer) => T) {
    (listeners[event] ??= []).push([cb as NfmwListener, converter]);
  },
  off<T = unknown>(event: string, cb: NfmwListener<T>) {
    const arr = listeners[event];
    if (arr) {
      listeners[event] = arr.filter(([h]) => h !== cb);
    }
  },
  emit<T = unknown>(event: string, data: T) {
    listeners[event]?.forEach(([h, converter]) => {
        h(converter ? converter(data as unknown as ArrayBuffer) : data)
    });
  },
};

// __nfmwDispatch is the primary C#→JS push channel.
window.__nfmwDispatch = function (event, data) {
  if (typeof data === 'string')
    window.nfmwEvents.emit(event, JSON.parse(data));
  else if (data instanceof ArrayBuffer)
    window.nfmwEvents.emit(event, data);
  else
    console.warn("[WARN] C# → JS event data is not string or ArrayBuffer:", event, data);
};

/**
 * Subscribe to a C# → JS push event for this phase.
 * Events are named "{phaseId}:{eventType}".
 *
 * @example
 *   onNfmwEvent("main-menu:account", (account: AccountData) => {
 *     setAccount(account);
 *   });
 */
export function onNfmwEvent<T = unknown>(
  event: string,
  callback: NfmwListener<T>,
  converter?: (buffer: ArrayBuffer) => T
): () => void {
  window.nfmwEvents.on(event, callback, converter);
  return () => window.nfmwEvents.off(event, callback);
}

/**
 * Send a JS → C# message. The first argument is the method name;
 * the second (optional) is a JSON payload.
 */
export function callNfmw(method: string, payload?: unknown): void {
  const json = payload != null ? JSON.stringify(payload) : undefined;
  window.__nfmwCall(method, json);
}
