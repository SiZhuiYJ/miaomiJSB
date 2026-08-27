import {
   createCustomBulletMmdPhysicsBackend,
   loadCustomBulletMmdModule,
   type CustomBulletMmdPhysicsBackend,
} from '@yohawing/three-mmd-loader/physics'

import {
   ThreeMmdLoader,
   type ThreeMmdModel,
} from '@yohawing/three-mmd-loader'

export interface MmdPhysicsOptions {
   /**
      * 是否启用物理
         */
   enabled?: boolean

   /**
      * Bullet 脚本地址
         *
            * Vite 推荐放在 public/mmd/mmd_bullet.js
               */
   scriptUrl?: string

   /**
      * MMD 时间步长
         */
   fixedTimeStep?: number

   /**
      * 最大子步数
         */
   maxSubSteps?: number

   /**
      * 是否启用 IK
         */
   ik?: boolean

   /**
      * MMD 帧率
         */
   frameRate?: number
}

export class MmdPhysicsController {

   private bulletModule: Awaited<
      ReturnType<typeof loadCustomBulletMmdModule>
   > | null = null

   private backend: CustomBulletMmdPhysicsBackend | null = null

   private initialized = false

   private disposed = false

   readonly options: Required<MmdPhysicsOptions>

   constructor(options: MmdPhysicsOptions = {}) {

      this.options = {
         enabled: options.enabled ?? true,

         scriptUrl:
            options.scriptUrl ??
            '/mmd/mmd_bullet.js',

         fixedTimeStep:
            options.fixedTimeStep ??
            1 / 60,

         maxSubSteps:
            options.maxSubSteps ??
            4,

         ik:
            options.ik ??
            true,

         frameRate:
            options.frameRate ??
            30,
      }
   }

   /**
      * 初始化 Bullet
         */
   async init(): Promise<void> {

      if (this.initialized) {
         return
      }

      if (this.disposed) {
         throw new Error(
            'MmdPhysicsController 已经被销毁',
         )
      }

      if (!this.options.enabled) {

         this.initialized = true

         return
      }

      console.log(
         '[MMD Physics] loading Bullet:',
         this.options.scriptUrl,
      )

      this.bulletModule =
         await loadCustomBulletMmdModule({
            scriptUrl: this.options.scriptUrl,
         })

      console.log(
         '[MMD Physics] Bullet loaded',
      )

      this.backend =
         createCustomBulletMmdPhysicsBackend(
            this.bulletModule,
            {
               fixedTimeStep:
                  this.options.fixedTimeStep,

               maxSubSteps:
                  this.options.maxSubSteps,
            },
         )

      this.initialized = true
   }

   /**
      * 创建带 Bullet Physics 的 MMD Loader
         */
   createLoader(): ThreeMmdLoader {

      if (!this.initialized) {

         throw new Error(
            'MmdPhysicsController 尚未初始化，请先 await init()',
         )
      }

      return new ThreeMmdLoader({

         runtime: {

            frameRate:
               this.options.frameRate,

            physics:
               this.options.enabled
                  ? 'external'
                  : 'none',

            physicsBackend:
               this.options.enabled
                  ? this.backend ?? undefined
                  : undefined,
         },
      })
   }

   /**
      * 更新模型
         */
   update(
      model: ThreeMmdModel,
      seconds: number,
   ): void {

      if (this.disposed) {
         return
      }

      model.update(
         seconds,
         {
            physics:
               this.options.enabled,

            ik:
               this.options.ik,
         },
      )
   }

   /**
      * 重置物理状态
         */
   reset(
      model: ThreeMmdModel,
   ): void {

      if (this.disposed) {
         return
      }

      model.runtime.resetPose()

      model.runtime.seek(0)

      this.backend?.reset?.()
   }

   /**
      * 获取 Bullet Backend
         */
   getBackend() {

      return this.backend
   }

   /**
      * 是否初始化
         */
   get isInitialized() {

      return this.initialized
   }

   /**
      * 销毁
         */
   dispose(): void {

      if (this.disposed) {
         return
      }
      this.backend?.dispose?.()

      this.backend = null

      this.bulletModule = null

      this.initialized = false

      this.disposed = true
   }
}