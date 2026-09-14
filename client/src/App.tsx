import { useState } from "react";
import "./App.css";
import type { Item } from "./components/item/item-model";

function App() {
  const [item, setItem] = useState<Item | null>(null);

  async function getItem() {
    try {
      const response = await fetch("http://localhost:5077/items", {
        method: "POST",
      });

      setItem(await response.json());
    } catch (error) {
      console.error("Error fetching item:", error);
    }
  }

  return (
    <>
      <div className="header">
        <h1>Super Mega Unbox Deluxe 3000</h1>
      </div>
      <div className="main-content">
        <button onClick={getItem}>Press this BUTTON to get an ITEM</button>
      </div>
      {item && (
        <>
          <div>{item?.name}</div>
          <div>{item?.value}</div>
          <div>{item?.rarity}</div>
          <div>{item?.stats.join(", ")}</div>
          <div>{item?.modifiers.join(", ")}</div>
          <div>{item?.dateGotten}</div>
          <img src={item?.imageKey} alt={item?.name} />
        </>
      )}
    </>
  );
}

export default App;
