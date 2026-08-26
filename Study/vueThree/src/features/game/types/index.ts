// types/mmd.ts
export interface CharacterConfig {
  id: string
  name: string
  modelPath: string
  // 可选：默认动作路径
  defaultMotion?: string
}

export interface MotionConfig {
  id: string
  name: string
  path: string
}