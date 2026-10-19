import { useEffect, useState } from "react";
import "./App.css";
import { ItemCard } from "./components/item/ItemCard";
import type { Item, LootSource } from "./components/item/item-model";

const apiUrl = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

async function readResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.detail ?? problem?.title ?? `Request failed (${response.status}).`);
  }
  return response.json() as Promise<T>;
}

function App() {
  const [sources, setSources] = useState<LootSource[]>([]);
  const [sourceId, setSourceId] = useState("");
  const [item, setItem] = useState<Item | null>(null);
  const [loadingSources, setLoadingSources] = useState(true);
  const [opening, setOpening] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [retry, setRetry] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    fetch(`${apiUrl}/loot-sources`, { signal: controller.signal })
      .then(readResponse<LootSource[]>)
      .then((loadedSources) => {
        if (controller.signal.aborted) return;
        if (loadedSources.length === 0) throw new Error("No loot sources are available.");
        setSources(loadedSources);
        setSourceId(loadedSources[0].id);
      })
      .catch((cause: unknown) => {
        if (!controller.signal.aborted) {
          setError(cause instanceof Error ? cause.message : "Could not load loot sources.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoadingSources(false);
      });
    return () => controller.abort();
  }, [retry]);

  async function getItem() {
    if (opening || !sourceId) return;
    setOpening(true);
    setError(null);
    try {
      const response = await fetch(`${apiUrl}/items`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ lootSourceId: sourceId }),
      });
      setItem(await readResponse<Item>(response));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not open this loot source.");
    } finally {
      setOpening(false);
    }
  }

  return (
    <main className="loot-page">
      <header>
        <p>Super Mega Unbox Deluxe 3000</p>
        <h1>Questionable loot. Impeccable taste.</h1>
      </header>
      <section className="loot-controls" aria-label="Open a loot source" aria-busy={loadingSources || opening}>
        <label htmlFor="loot-source">Loot source</label>
        <select id="loot-source" value={sourceId} disabled={loadingSources || opening || sources.length === 0}
          onChange={(event) => setSourceId(event.target.value)}>
          {sources.length === 0 && <option value="">{loadingSources ? "Loading sources…" : "Sources unavailable"}</option>}
          {sources.map((source) => <option key={source.id} value={source.id}>{source.name}</option>)}
        </select>
        <p>{sources.find((source) => source.id === sourceId)?.description}</p>
        <button onClick={getItem} disabled={loadingSources || opening || !sourceId}>
          {opening ? "Rummaging…" : "Open for free"}
        </button>
        {error && <p role="alert" className="error">{error}</p>}
        {!loadingSources && sources.length === 0 && (
          <button onClick={() => { setLoadingSources(true); setError(null); setRetry(retry + 1); }}>Retry loading sources</button>
        )}
      </section>
      <section aria-live="polite" aria-busy={opening}>
        {item ? <ItemCard item={item} /> : <p className="empty-state">Your next terrible treasure awaits.</p>}
      </section>
    </main>
  );
}

export default App;
