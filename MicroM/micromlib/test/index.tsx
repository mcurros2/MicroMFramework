import "@mantine/core/styles.css";
import "@mantine/dates/styles.css";
import "@mantine/dropzone/styles.css";
import "@mantine/spotlight/styles.css";

import { createRoot } from "react-dom/client"
import { App } from "./App"
import { StrictMode } from "react"

const root = createRoot(document.getElementById("app")!)
root.render(
    <StrictMode>
        <App />
    </StrictMode>
)