import { useState } from "react";
import "./App.css";

function App() {
  const [item, setItem] = useState(null);

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
      <button onClick={getItem}>Get Item</button>
      <div>{item}</div>
    </>
  );
}

export default App;
