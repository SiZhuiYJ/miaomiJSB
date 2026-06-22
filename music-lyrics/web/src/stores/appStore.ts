import { defineStore } from 'pinia'
import type { AudioQuality, LyricFormat, Provider, SearchType } from '../api'

export interface SearchHistoryItem {
  id: string
  keyword: string
  provider: Provider
  type: SearchType
  createdAt: number
}

interface AppState {
  cookies: {
    netEaseCookie: string
    qqCookie: string
  }
  pageOptions: {
    keyword: string
    provider: Provider
    type: SearchType
    lyricFormat: LyricFormat
    includeTranslation: boolean
    includeTransliteration: boolean
    audioQuality: AudioQuality
    converterFrom: LyricFormat
    converterTo: LyricFormat
    converterDurationMs: number
  }
  searchHistory: SearchHistoryItem[]
}

const defaultPageOptions: AppState['pageOptions'] = {
  keyword: '',
  provider: 'netease',
  type: 'song',
  lyricFormat: 'lrc',
  includeTranslation: true,
  includeTransliteration: false,
  audioQuality: 'standard',
  converterFrom: 'lrc',
  converterTo: 'srt',
  converterDurationMs: 0,
}

export const useAppStore = defineStore('app', {
  state: (): AppState => ({
    cookies: {
      netEaseCookie: '',
      qqCookie: '',
    },
    pageOptions: { ...defaultPageOptions },
    searchHistory: [],
  }),
  actions: {
    addSearchHistory(input: { keyword: string; provider: Provider; type: SearchType }) {
      const keyword = input.keyword.trim()
      if (!keyword) return

      const next: SearchHistoryItem = {
        id: `${input.provider}:${input.type}:${keyword.toLowerCase()}`,
        keyword,
        provider: input.provider,
        type: input.type,
        createdAt: Date.now(),
      }

      this.searchHistory = [
        next,
        ...this.searchHistory.filter((item) => item.id !== next.id),
      ].slice(0, 12)
    },
    clearSearchHistory() {
      this.searchHistory = []
    },
    clearCookies() {
      this.cookies.netEaseCookie = ''
      this.cookies.qqCookie = ''
    },
    resetPageOptions() {
      this.pageOptions = { ...defaultPageOptions }
    },
  },
})
