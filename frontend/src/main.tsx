import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './index.css';

// Startpunkt – monterar React-appen i div#root i index.html
ReactDOM.createRoot(document.getElementById('root')!).render(
  // StrictMode kör extra kontroller i dev (dubbel-render etc.)
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
