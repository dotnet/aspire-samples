import React from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import App from "./components/App";

const container = document.getElementById("root");
const root = createRoot(container);
const apiserver =
  variables.REACT_APP_WEATHER_API_HTTPS ||
  variables.REACT_APP_WEATHER_API_HTTP;
root.render(
  <React.StrictMode>
    <App weatherApi={`${apiserver}/weatherforecast`} />
  </React.StrictMode>
);
