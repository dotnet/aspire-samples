/// <reference types="vite/client" />

interface ImportMetaEnv {
    readonly VITE_WEATHER_API_HTTPS: string,
    readonly VITE_WEATHER_API_HTTP: string
}

interface ImportMeta {
    readonly env: ImportMetaEnv
}
