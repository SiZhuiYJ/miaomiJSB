import { defineStore } from "pinia";
import { ref, computed, watch } from "vue";
import type {
  CharacterConfig,
  MotionConfig
} from "@/features/game/types";
// ========== 角色和动作配置 ==========
const characters: CharacterConfig[] = [
  {
    id: 'hutao',
    name: '胡桃',
    modelPath: 'public/models/hutao/hutao/hutao.pmx',
    defaultMotion: 'public/models/hutao/move/ayaka-dance.vmd'
  },
  {
    id: 'muou',
    name: '桑多涅',
    modelPath: 'public/models/mmd/【桑多涅】_by_原神_e06bb339ac99ae18f7cf88d619e9b975/桑多涅.pmx',
    defaultMotion: 'public/models/hutao/move/ayaka-dance.vmd'
  },
  {
    id: 'liuying',
    name: '流萤',
    modelPath: 'public/models/mmd/星穹铁道—流萤·春日手信_by_崩坏：星穹铁道_948486d4ddcc6988bd90019585983d7a/星穹铁道—流萤·春日手信/星穹铁道—流萤·春日手信.pmx',
    defaultMotion: 'public/models/hutao/move/ayaka-dance.vmd'
  },
  {
    id: 'leimiaier-white',
    name: '蕾米埃尔·白',
    modelPath: 'public/models/mmd/3.1蕾米埃尔_by_绝区零_b01b5cf19c83a57d763b11a0995c74b3/蕾米埃尔-白/蕾米埃尔-白.pmx',
    defaultMotion: 'public/models/hutao/move/ayaka-dance.vmd'
  },
  {
    id: 'leimiaier-black',
    name: '蕾米埃尔·黑',
    modelPath: 'public/models/mmd/3.1蕾米埃尔_by_绝区零_b01b5cf19c83a57d763b11a0995c74b3/蕾米埃尔-黑/蕾米埃尔-黑.pmx',
    defaultMotion: 'public/models/hutao/move/ayaka-dance.vmd'
  },
  {
    id: 'leimiaier-swimsuit',
    name: '蕾米埃尔·泳装',
    modelPath: 'public/models/mmd/3.1蕾米埃尔_by_绝区零_b01b5cf19c83a57d763b11a0995c74b3/蕾米埃尔-泳装/蕾米埃尔-泳装.pmx',
    defaultMotion: 'public/models/hutao/move/ayaka-dance.vmd'
  },
  {
    id: 'fengjing',
    name: '风堇',
    modelPath: 'public/models/mmd/星穹铁道—风堇（含武器）_by_崩坏：星穹铁道_f83f23e3cbce69f9982d3589847ccf01/星穹铁道—风堇3.pmx',
    defaultMotion: 'public/models/hutao/move/ayaka-dance.vmd'
  },
  {
    id: 'xiaoyika',
    name: '小伊卡',
    modelPath: 'public/models/mmd/星穹铁道—风堇（含武器）_by_崩坏：星穹铁道_f83f23e3cbce69f9982d3589847ccf01/小伊卡/星穹铁道—小伊卡.pmx',
    defaultMotion: 'public/models/hutao/move/ayaka-dance.vmd'
  },
  {
    id: 'wanguiren',
    name: '忘归人',
    modelPath: 'public/models/mmd/星穹铁道—忘归人_by_崩坏：星穹铁道_6c302ffb9750e930ea3b89cd1fbba340/星穹铁道—忘归人.pmx',
    defaultMotion: 'public/models/hutao/move/ayaka-dance.vmd'
  },
  {
    id: 'xiadie',
    name: '遐蝶',
    modelPath: 'public/models/mmd/星穹铁道—遐蝶·幽梦翩跹_by_崩坏：星穹铁道_27d4e869ee96e8eb05a76b35d49a7dbd/星穹铁道—遐蝶.pmx',
    defaultMotion: 'public/models/hutao/move/ayaka-dance.vmd'
  },
]

const motions: MotionConfig[] = [
  { id: 'arrogant', name: '嚣张', path: 'public/models/hutao/move/荧-嚣张.vmd' },
  { id: 'idle', name: '舞蹈', path: 'public/models/hutao/move/ayaka-dance.vmd' },
  { id: 'face_60fps', name: '妄想天使动作face', path: 'public/models/move/妄想天使动作+镜头_by_DingBuDoa/face_60fps.vmd' },
  { id: 'TDA_60fps', name: '妄想天使动作TDA', path: 'public/models/move/妄想天使动作+镜头_by_DingBuDoa/TDA_60fps.vmd' },
]
export const useGameStore = defineStore("game", () => {
  const currentCharacterId = ref<string>(characters[0]!.id)
  const currentMotionId = ref<string>(motions[0]!.id)

  function GetCharacterById(id: string): CharacterConfig | undefined {
    return characters.find(c => c.id === id);
  }

  function GetMotionById(id: string): MotionConfig | undefined {
    return motions.find(m => m.id === id);
  }

  // 计算属性
  const currentCharacter = computed(() => {
    return characters;
  });

  const currentMotion = computed(() => {
    return motions;
  });

  return {
    currentCharacterId,
    currentMotionId,
    currentCharacter,
    currentMotion,
    GetCharacterById,
    GetMotionById
  };
},
  {
    // // 持久化配置
    persist: {
      // 持久化存储的键名
      key: "game",
      // 存储方式：localStorage
      storage: localStorage,
      // 指定持久化字段
      pick: [
        "currentCharacterId",
        "currentMotionId",
      ],
    },
  });
