import * as THREE from 'three';
import type { ThreeMmdModel } from '@yohawing/three-mmd-loader';

/**
 * 递归释放 Three.js 对象及其子对象的 GPU 资源（几何体、材质、纹理）
 */
function disposeObject(obj: THREE.Object3D): void {
    if (!obj) return;

    // 处理网格
    if (obj instanceof THREE.Mesh) {
        if (obj.geometry) {
            obj.geometry.dispose();
        }

        const materials = Array.isArray(obj.material) ? obj.material : [obj.material];
        for (const material of materials) {
            if (material.map) material.map.dispose();
            if (material.lightMap) material.lightMap.dispose();
            if (material.bumpMap) material.bumpMap.dispose();
            if (material.normalMap) material.normalMap.dispose();
            if (material.specularMap) material.specularMap.dispose();
            material.dispose();
        }
    }

    // 递归处理子对象
    while (obj.children.length > 0) {
        const child = obj.children[0];
        if (child) {
            disposeObject(child);
            obj.remove(child);
        }
    }
}

/**
 * LRU 缓存配置
 */
export interface ModelCacheOptions {
    maxSize?: number;          // 最大缓存数量，默认 3
}

/**
 * 模型缓存类 - 管理 ThreeMmdModel 的 LRU 缓存
 * 自动淘汰最久未使用的模型，并释放 GPU 资源
 */
export class ModelCache {
    private cache = new Map<string, ThreeMmdModel>();
    private accessOrder: string[] = []; // 按访问时间排序，尾部为最新
    private maxSize: number;

    constructor(options: ModelCacheOptions = {}) {
        this.maxSize = options.maxSize ?? 3;
    }

    /**
     * 获取缓存的模型，并更新访问顺序
     */
    get(id: string): ThreeMmdModel | undefined {
        const model = this.cache.get(id);
        if (model) {
            this.touch(id);
        }
        return model;
    }

    /**
     * 存入模型，如果达到上限则淘汰最旧的模型
     * 返回被淘汰的模型（如果有），调用方需将其从场景移除
     */
    set(id: string, model: ThreeMmdModel): ThreeMmdModel | undefined {
        // 如果已存在，先更新访问顺序
        if (this.cache.has(id)) {
            this.touch(id);
            // 但模型对象可能不同（覆盖），直接替换
            this.cache.set(id, model);
            return undefined;
        }

        // 检查是否超限，淘汰旧模型
        let evicted: ThreeMmdModel | undefined;
        while (this.accessOrder.length >= this.maxSize) {
            const oldestId = this.accessOrder.shift();
            if (oldestId) {
                const oldModel = this.cache.get(oldestId);
                if (oldModel) {
                    evicted = oldModel;
                    this.cache.delete(oldestId);
                    // 释放 GPU 资源（调用方需确保已从场景移除）
                    this.disposeModel(oldModel);
                }
            }
        }

        // 存入新模型
        this.cache.set(id, model);
        this.touch(id);
        return evicted;
    }

    /**
     * 删除指定模型（立即释放资源）
     */
    delete(id: string): boolean {
        const model = this.cache.get(id);
        if (!model) return false;
        this.cache.delete(id);
        const index = this.accessOrder.indexOf(id);
        if (index !== -1) this.accessOrder.splice(index, 1);
        this.disposeModel(model);
        return true;
    }

    /**
     * 清空所有缓存
     */
    clear(): void {
        for (const [id, model] of this.cache) {
            this.disposeModel(model);
        }
        this.cache.clear();
        this.accessOrder = [];
    }

    /**
     * 获取当前缓存数量
     */
    get size(): number {
        return this.cache.size;
    }

    /**
     * 修改最大缓存数量（会触发立即淘汰）
     */
    setMaxSize(newSize: number): void {
        if (newSize < 1) newSize = 1;
        this.maxSize = newSize;
        // 如果当前缓存超限，立即淘汰
        while (this.accessOrder.length > this.maxSize) {
            const oldestId = this.accessOrder.shift();
            if (oldestId) {
                const oldModel = this.cache.get(oldestId);
                if (oldModel) {
                    this.cache.delete(oldestId);
                    this.disposeModel(oldModel);
                }
            }
        }
    }

    /**
     * 更新访问顺序（将 id 移到尾部）
     */
    private touch(id: string): void {
        const index = this.accessOrder.indexOf(id);
        if (index !== -1) {
            this.accessOrder.splice(index, 1);
        }
        this.accessOrder.push(id);
    }

    /**
     * 释放模型占用的 GPU 内存
     */
    private disposeModel(model: ThreeMmdModel): void {
        if (model.root) {
            disposeObject(model.root);
        }
        if (typeof (model as any).dispose === 'function') {
            (model as any).dispose();
        }
    }
}

// 导出默认单例实例（可选）
export const defaultModelCache = new ModelCache();