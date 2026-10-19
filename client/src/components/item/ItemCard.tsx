import type { Item } from "./item-model";

export function ItemCard({ item }: { item: Item }) {
  return (
    <article className="item-card">
      <div className="item-art" role="img" aria-label="Item artwork placeholder">?</div>
      <p className="item-rarity">{item.rarity} · {item.itemBase}</p>
      <h2>{item.name}</h2>
      <dl className="item-stats">
        {item.stats.map((stat) => <div key={stat.name}><dt>{stat.name}</dt><dd>{stat.value}</dd></div>)}
      </dl>
      {item.modifiers.length > 0 && (
        <ul className="item-modifiers">
          {item.modifiers.map((modifier, index) => (
            <li key={modifier.details?.definitionId ?? index}>
              <strong>{modifier.name}</strong>
              <p>{modifier.value}</p>
            </li>
          ))}
        </ul>
      )}
      <footer>
        <p>Value: {item.value.toLocaleString()} coins</p>
        <p>Acquired <time dateTime={item.dateGotten}>{new Date(item.dateGotten).toLocaleString()}</time></p>
      </footer>
    </article>
  );
}
