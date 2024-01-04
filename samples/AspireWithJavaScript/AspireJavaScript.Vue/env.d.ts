/// <reference types="vite/client" />

interface ImportMetaEnv {
    readonly VITE_WEATHER_API: string
}

interface ImportMeta {
    readonly env: ImportMetaEnv
}
