import type { RouteRecordRaw } from "vue-router";
import { HOME_URL, LOGIN_URL } from "@/config";
import Layout from "@/layouts/Frontend/index.vue";

/**
 * LayoutRouter[布局路由]
 */
export const layoutRouter: RouteRecordRaw[] = [
  {
    path: "/",
    redirect: HOME_URL,
  },
  // {
  //   path: "/layout",
  //   name: "layout",
  //   component: Layout,
  //   redirect: HOME_URL,
  //   children: [
  //      {
  //       path: HOME_URL, // [唯一]
  //       component: () => import("@/views/home/index.vue"),
  //       meta: {
  //         title: "主控台", // 标题
  //         enName: "Master Station", // 英文名称
  //         icon: "HomeFilled", // 图标
  //         isHide: "1", // 代表路由在菜单中是否隐藏，是否隐藏[0隐藏，1显示]
  //         isLink: "", // 是否外链[有值则是外链]
  //         isKeepAlive: "0", // 是否缓存路由数据[0是，1否]
  //         isFull: "1", // 是否缓存全屏[0是，1否]
  //         isAffix: "0" // 是否缓存固定路由[0是，1否]
  //       }
  //     }
  //   ]
  // },
  // 上方或者下方效果一样
  {
    path: "/layout",
    name: "layout",
    component: Layout,
    redirect: HOME_URL,
    children: [
      {
        path: HOME_URL, // [唯一]
        component: () => import("@/views/main/home/index.vue"),
        meta: {
          title: "首页", // 标题
          enName: "Master Station", // 英文名称
          isHeader: "0",// 代表路由在头部显示[0隐藏，1显示]
          icon: "HomeFilled", // 图标
          isHide: "1", // 代表路由在菜单中是否隐藏，是否隐藏[0隐藏，1显示]
          isLink: "", // 是否外链[有值则是外链]
          isKeepAlive: "0", // 是否缓存路由数据[0是，1否]
          isFull: "1", // 是否缓存全屏[0是，1否]
          isAffix: "0", // 是否缓存固定路由[0是，1否]
        },
      },

      {
        path: "/main/blogPost",
        component: () => import("@/views/main/blogPost/directory/index.vue"),
        meta: {
          title: "文档",
          enName: "blogPost",
          isHeader: "0",
          icon: "Clock",
          isHide: "1",
          isLink: "",
          isKeepAlive: "0",
          isFull: "1",
          isAffix: "0",
        },
      },
      {
        path: "/main/classSchedule",
        component: () => import("@/views/main/classSchedule/index.vue"),
        meta: {
          title: "课表",
          enName: "classSchedule",
          isHeader: "0",
          icon: "Calendar",
          isHide: "1",
          isLink: "",
          isKeepAlive: "0",
          isFull: "1",
          isAffix: "0",
        },
      },
      {
        path: "/main/blogPost/article",
        component: () => import("@/views/main/blogPost/article/index.vue"),
        meta: {
          title: "博客内容",
          enName: "blogPost",
          isHeader: "0",
          icon: "Clock",
          isHide: "0",
          isLink: "",
          isKeepAlive: "0",
          isFull: "1",
          isAffix: "0",
        },
      },

      {
        path: "/main/animation",
        name: "animation",
        component: () => import("@/views/main/animation/index.vue"),
        meta: {
          title: "动画",
          enName: "animation",
          icon: "MeowMemoirs-animation",
          isHide: "1",
          isLink: "",
          isKeepAlive: "0",
          isFull: "1",
          isAffix: "0",
        },
        children: [
          {
            path: "/main/animation/timeStyle",
            component: () =>
              import("@/views/main/animation/timeStyle/index.vue"),
            meta: {
              title: "时间",
              enName: "TimeStyle",
              isHeader: "0",
              icon: "Clock",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
          {
            path: "/main/animation/starrySky",
            component: () =>
              import("@/views/main/animation/starrySky/index.vue"),
            meta: {
              title: "星空",
              enName: "StarrySky",
              isHeader: "0",
              icon: "Star",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },

          {
            path: "/main/animation/banner",
            component: () => import("@/views/main/animation/banner/index.vue"),
            meta: {
              title: "蛋糕",
              enName: "banner",
              icon: "IceTea",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
          {
            path: "/main/animation/love",
            component: () => import("@/views/main/animation/love/index.vue"),
            meta: {
              title: "爱心",
              enName: "love",
              icon: "MeowMemoirs-love",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
        ],
      },
      {
        path: "/main/confession",
        name: "confession",
        component: () => import("@/views/main/confession/index.vue"),
        meta: {
          title: "告白",
          enName: "confession of love",
          icon: "Promotion",
          isHide: "1",
          isLink: "",
          isKeepAlive: "0",
          isFull: "1",
          isAffix: "0",
        },
        children: [
          {
            path: "/main/confession/love",
            component: () => import("@/views/main/confession/love/index.vue"),
            meta: {
              title: "表白",
              enName: "love",
              icon: "MeowMemoirs-cute",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
          {
            path: "/main/confession/certificate",
            component: () =>
              import("@/views/main/confession/certificate/index.vue"),
            meta: {
              title: "表白证书",
              enName: "certificate",
              icon: "MeowMemoirs-marriage-certificate",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
          {
            path: "/main/confession/settings",
            component: () =>
              import("@/views/main/confession/settings/index.vue"),
            meta: {
              title: "设置",
              enName: "settings",
              isHeader: "0",
              icon: "Tools",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
        ],
      },
      {
        path: "/main/memo",
        name: "memo",
        component: () => import("@/views/main/memo/index.vue"),
        meta: {
          title: "便签",
          enName: "memo",
          icon: "MeowMemoirs-sticky-note",
          isHide: "1",
          isLink: "",
          isKeepAlive: "0",
          isFull: "1",
          isAffix: "0",
        },

        children: [
          {
            path: "/main/memo/notes",
            component: () => import("@/views/main/memo/notes/index.vue"),
            meta: {
              title: "记事",
              enName: "notes",
              icon: "MeowMemoirs-memorandum",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
          {
            path: "/main/memo/todo",
            component: () => import("@/views/main/memo/todo/index.vue"),
            meta: {
              title: "待办",
              enName: "todo",
              icon: "MeowMemoirs-to-do",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
        ],
      }, {
        path: "/main/effect",
        name: "effect",
        component: () => import("@/views/main/effect/index.vue"),
        meta: {
          title: "案例",
          enName: "effect",
          icon: "MeowMemoirs-sticky-note",
          isHide: "1",
          isLink: "",
          isKeepAlive: "0",
          isFull: "1",
          isAffix: "0",
        },

        children: [
          {
            path: "/main/effect/ipLocation",
            component: () => import("@/views/main/effect/ipLocation/index.vue"),
            meta: {
              title: "地址解析",
              enName: "ipLocation",
              icon: "MeowMemoirs-memorandum",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },

          },
          {
            path: "/main/effect/musicPlayer",
            component: () => import("@/views/main/effect/musicPlayer/index.vue"),
            meta: {
              title: "音乐播放器",
              enName: "musicPlayer",
              icon: "Headset",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },

          {
            path: "/main/effect/shooting",
            component: () => import("@/views/main/effect/shooting/index.vue"),
            meta: {
              title: "拍照",
              enName: "shooting",
              icon: "Camera",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
          // 圣诞节
          {
            path: "/main/effect/christmas",
            component: () => import("@/views/main/effect/christmas/index.vue"),
            meta: {
              title: "圣诞节特效",
              enName: "christmas",
              icon: "MeowMemoirs-christmas-tree",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
          {
            path: "/main/effect/videoPlay",
            component: () => import("@/views/main/effect/videoPlay/index.vue"),
            meta: {
              title: "视频播放",
              enName: "videoPlay",
              icon: "VideoCamera",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          }, {
            path: "/main/effect/newVideoPlay",
            component: () => import("@/views/main/effect/newVideoPlay/index.vue"),
            meta: {
              title: "新*视频播放器",
              enName: "newVideoPlay",
              icon: "VideoCamera",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
          // countdown
          {
            path: "/main/effect/countdown",
            component: () => import("@/views/main/effect/countdown/index.vue"),
            meta: {
              title: "倒计时器",
              enName: "countdown",
              icon: "Time",
              isHeader: "0",
              isHide: "1",
              isLink: "",
              isKeepAlive: "0",
              isFull: "1",
              isAffix: "0",
            },
          },
        ],
      },
    ],
  },
  {
    path: LOGIN_URL,
    name: "login",
    component: () => import("@/views/login/index.vue"),
    meta: {
      title: "登录",
    },
  },
];
/**
 * staticRouter[静态路由]
 */
export const staticRouter: any = [
  /** 主控台 RouteRecordRaw[]*/
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
];

/**
 * errorRouter (错误页面路由)
 */
export const errorRouter = [
  {
    path: "/403",
    name: "403",
    component: () => import("@/views/error/403.vue"),
    meta: {
      title: "403页面",
      enName: "403 Page", // 英文名称
      icon: "QuestionFilled", // 菜单图标
      isHide: "1", // 代表路由在菜单中是否隐藏，是否隐藏[0隐藏，1显示]
      isLink: "1", // 是否外链[有值则是外链]
      isKeepAlive: "0", // 是否缓存路由数据[0是，1否]
      isFull: "1", // 是否缓存全屏[0是，1否]
      isAffix: "1", // 是否缓存固定路由[0是，1否]
    },
  },
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
  {
    path: "/500",
    name: "500",
    component: () => import("@/views/error/500.vue"),
    meta: {
      title: "500页面",
      enName: "500 Page", // 英文名称
      icon: "WarningFilled", // 图标
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
