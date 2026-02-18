/**
 * v-auth 权限控制指令
 * 
 * 用于控制元素的显示/隐藏，基于用户的权限列表进行判断
 * 
 * 使用方式：
 * 1. v-auth="'permission'" - 单个权限判断
 * 2. v-auth="['permission1', 'permission2']" - 多个权限判断（满足任一即可）
 * 
 * 权限控制逻辑：
 * - 如果用户拥有指定权限中的任何一个或者是管理员（拥有"*"权限），则显示元素
 * - 否则移除元素
 * 
 * @example
 * <!-- 单个权限 -->
 * <button v-auth="'user:create'">新增用户</button>
 * 
 * <!-- 多个权限 -->
 * <button v-auth="['user:create', 'user:edit']">用户管理</button>
 */

import { useAuthStore } from "@/stores";
import type { Directive, DirectiveBinding } from "vue";

/**
 * 权限控制指令实现
 */
const auth: Directive = {
  /**
   * 指令挂载时的处理函数
   * 
   * @param el 指令绑定的DOM元素
   * @param binding 指令绑定的相关信息
   */
  mounted(el: HTMLElement, binding: DirectiveBinding) {
    // 获取指令传入的值（权限列表）
    const { value } = binding;

    // 获取用户权限存储实例
    const userStore = useAuthStore();

    // 管理员权限（通配符）
    const adminButtons = ["*"];

    /**
     * 权限判断逻辑：
     * 1. 如果传入的是数组，检查用户是否拥有其中任一权限
     * 2. 或者用户是否拥有管理员权限
     */
    if (
      (Array.isArray(value) &&
        value.some((permission: string) =>
          userStore.buttonList.includes(permission)
        )) ||
      JSON.stringify(userStore.buttonList) === JSON.stringify(adminButtons)
    ) {
      // 如果用户拥有指定权限中的任何一个或者是管理员，则显示元素（不做任何操作）
    } else {
      // 如果用户不拥有所有权限，则从DOM中移除该元素
      el.parentNode?.removeChild(el);
    }
  },
};

// 导出权限指令
export default auth;