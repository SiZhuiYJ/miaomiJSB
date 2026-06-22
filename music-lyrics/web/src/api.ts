export type SearchType = 'song' | 'album' | 'playlist'
export type Provider = 'netease' | 'qq' | 'all'
export type LyricFormat = 'lrc' | 'srt'

export interface SearchItem {
  provider: 'netease' | 'qq'
  type: SearchType
  id: string
  title: string
  artists: string[]
  album?: string | null
  durationMs?: number | null
  coverUrl?: string | null
  songCount?: number | null
  description?: string | null
}

export interface SongInfo {
  provider: 'netease' | 'qq'
  id: string
  displayId: string
  title: string
  artists: string[]
  album: string
  durationMs: number
  coverUrl?: string | null
  publishDate?: string | null
}

export interface RawLyrics {
  lyric: string
  translation: string
  transliteration: string
  isEmpty: boolean
}

export interface LyricsResponse {
  song: SongInfo
  raw: RawLyrics
  format: LyricFormat
  output: string
}

export interface SearchResponse {
  items: SearchItem[]
}

export interface TrackResponse {
  tracks: SongInfo[]
}

export interface SongLinkResponse {
  url: string
  source: string
}

export interface ConvertLyricsResponse {
  output: string
}

interface ApiError {
  message?: string
  title?: string
  detail?: string
}

export async function postJson<T>(path: string, body: Record<string, unknown>): Promise<T> {
  const response = await fetch(path, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(stripEmpty(body)),
  })

  if (!response.ok) {
    let message = `请求失败：${response.status}`
    try {
      const error = (await response.json()) as ApiError
      message = error.message || error.detail || error.title || message
    } catch {
      message = await response.text()
    }
    throw new Error(message)
  }

  return (await response.json()) as T
}

function stripEmpty(input: Record<string, unknown>) {
  return Object.fromEntries(
    Object.entries(input).filter(([, value]) => value !== '' && value !== null && value !== undefined),
  )
}
