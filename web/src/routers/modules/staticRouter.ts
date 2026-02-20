import type { RouteRecordRaw } from "vue-router";
import { HOME_URL, LOGIN_URL } from "@/config/index";
import main from "@/views/main/index.vue";

/**
 * staticRouter[静态路由]
 */
export const staticRouter: RouteRecordRaw[] = [
  {
    path: "/",
    redirect: HOME_URL,
  },
  /** 主控台 RouteRecordRaw[]*/
  {
    path: "/main",
    name: "main",
    component: main,
    redirect: HOME_URL,
    children: [
      {
        path: HOME_URL, // [唯一]
        component: () => import("@/views/main/home/index.vue"),
        meta: {
          title: "首页", // 标题
          enName: "Master Station", // 英文名称
          icon: "HomeFilled", // 图标 HomeFilled
          isHide: "1", // 代表路由在菜单中是否隐藏，是否隐藏[0隐藏，1显示]
          isLink: "", // 是否外链[有值则是外链]
          isKeepAlive: "0", // 是否缓存路由数据[0是，1否]
          isFull: "1", // 是否缓存全屏[0是，1否]
          isAffix: "0", // 是否缓存固定路由[0是，1否]
        },
      },
      {
        path: "/setting", // [唯一]
        component: () => import("@/views/main/setting/index.vue"),
        meta: {
          title: "设置", // 标题
          enName: "Master Station", // 英文名称
          icon: "HomeFilled", // 图标 HomeFilled
          isHide: "1", // 代表路由在菜单中是否隐藏，是否隐藏[0隐藏，1显示]
          isLink: "", // 是否外链[有值则是外链]
          isKeepAlive: "0", // 是否缓存路由数据[0是，1否]
          isFull: "1", // 是否缓存全屏[0是，1否]
          isAffix: "0", // 是否缓存固定路由[0是，1否]
        },
      },
    ],
  },
  {
    path: LOGIN_URL,
    name: "login",
    component: () => import("@/views/auth/index.vue"),
    meta: {
      title: "登录",
    },
  },
];

/**
 * errorRouter (错误页面路由)
 */
export const errorRouter = [
  {
    path: "/404",
    name: "404",
    component: () => import("@/views/error/404.vue"),
    meta: {
      title: "404页面",
      enName: "404 Page", // 英文名称
      icon: "CircleCloseFilled", // 菜单图标
      isHide: "1", // 代表路由在菜单中是否隐藏，是否隐藏[0隐藏，1显示]
      isLink: "1", // 是否外链[有值则是外链]
      isKeepAlive: "0", // 是否缓存路由数据[0是，1否]
      isFull: "1", // 是否缓存全屏[0是，1否]
      isAffix: "1", // 是否缓存固定路由[0是，1否]
    },
  },
  // 找不到path将跳转404页面
  {
    path: "/:pathMatch(.*)*",
    component: () => import("@/views/error/404.vue"),
  },
];
