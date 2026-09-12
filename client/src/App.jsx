import { useState } from "react";
import "./App.css";

function App() {
  const [item, setItem] = useState(null);

  async function getItem() {
    const response = await fetch("http://localhost:5077/items", {
      method: "POST",
      body: null,
      headers: {
        Host: "localhost:5077",
      },
    }).catch((error) => {
      console.error("Error fetching item:", error);
    });
    console.log(response);
    const data = await response.json();
    setItem(data);
  }

  return (
    <>
      <button onClick={getItem}>Get Item</button>
      <div>{item}</div>
    </>
  );
}

export default App;
