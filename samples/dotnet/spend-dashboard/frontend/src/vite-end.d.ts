/// <reference types="vite/client" />
declare const GITHUB_RUNTIME_PERMANENT_NAME: string
declare const BASE_KV_SERVICE_URL: string

declare global {
  interface Window {
    spark: {
      llmPrompt: (strings: string[], ...values: any[]) => string
      llm: (prompt: string, modelName?: string, jsonMode?: boolean) => Promise<string>
      user: () => Promise<{ avatarUrl: string; email: string; id: string; isOwner: boolean; login: string }>
      kv: {
        keys: () => Promise<string[]>
        get: <T>(key: string) => Promise<T | undefined>
        set: <T>(key: string, value: T) => Promise<void>
        delete: (key: string) => Promise<void>
      }
    }
  }
  
  const spark: Window['spark'];

  interface ImportMetaEnv {
    readonly VITE_API_BASE_URL?: string
  }
  interface ImportMeta {
    readonly env: ImportMetaEnv
  }
}