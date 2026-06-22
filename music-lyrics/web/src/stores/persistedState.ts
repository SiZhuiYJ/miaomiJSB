import type { PiniaPluginContext } from 'pinia'

const STORAGE_PREFIX = 'music-lyrics:pinia:'

export function persistedStatePlugin({ store }: PiniaPluginContext) {
  if (typeof window === 'undefined') return

  const key = `${STORAGE_PREFIX}${store.$id}`
  const raw = window.localStorage.getItem(key)

  if (raw) {
    try {
      store.$patch(JSON.parse(raw))
    } catch {
      window.localStorage.removeItem(key)
    }
  }

  store.$subscribe(
    (_mutation, state) => {
      window.localStorage.setItem(key, JSON.stringify(state))
    },
    { detached: true },
  )
}
