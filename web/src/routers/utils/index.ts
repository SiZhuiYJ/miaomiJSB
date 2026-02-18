import Default from "@/layouts/components/default/index.vue";
import router from "@/routers/index.ts";
import { HOME_URL } from "@/config/index.ts";
import type { AppRouteRecordRaw } from "@/routers/type";
import { markRaw, type DefineComponent } from "vue";
import type { menuList } from "@/libs/api/login/user/type";

/**
 * @description 使用递归过滤出需要渲染在左侧菜单动态数据的列表 (需剔除 isHide == 0 隐藏的菜单)
 * @param {Array} menuList 菜单列表
 * @returns {Array}
 * */
export function getShowStaticAndDynamicMenuList(menuList: menuList[]) {
  let newMenuList: menuList[] = JSON.parse(JSON.stringify(menuList));
  return newMenuList.filter((item: menuList) => {
    return item.isHide == "1";
  });
}

/**
 * 注意：使用console.log("路由数据", JSON.stringify(generateRoutes(res.data, 0))打印会发现子路由的component打印不出来，JSON不能打印出来函数。${data[i].component}
 */
// 递归函数用于生成路由配置，登录的时候也需要调用一次。
export function generateRoutes(data: menuList[], parentId: string | number) {
  // 首先把你需要动态路由的组件地址全部获取[vue2中可以直接用拼接的方式，但是vue3中必须用这种方式]
  // let modules = import.meta.glob("@/views/**/*.vue");
  // 类型安全的动态导入处理器
  const modules = import.meta.glob("/src/views/**/*.vue") as Record<
    string,
    () => Promise<{ default: DefineComponent }>
  >;
  // 处理动态导入模块的类型
  const routeList: AppRouteRecordRaw[] = [];
  // 如果 data 为空，直接返回空数组
  if (!data || data.length === 0) {
    return routeList;
  }
  for (var i = 0; i < data.length; i++) {
    if (data[i] && !router.hasRoute(data[i].path)) {
      if (data[i].parentId === parentId) {
        const componentPath = `/src/views/${data[i].component}.vue`;
        // 处理动态组件
        const asyncComponent = modules[componentPath]
          ? () => modules[componentPath]()
          : undefined;
        const route: AppRouteRecordRaw = {
          path: `${data[i].path}`,
          name: `${data[i].name}`,
          // 这里modules[`/src/views/${componentTemplate}.vue`] 一定要用绝对定位
          component: asyncComponent || markRaw(Default as DefineComponent),
          meta: {
            title: data[i]?.menuName,
            enName: data[i]?.enName,
            icon: data[i]?.icon,
            isHide: data[i]?.isHide,
            isKeepAlive: data[i]?.isKeepAlive,
            isLink: data[i]?.isLink,
            isFull: data[i]?.isFull,
            isAffix: data[i]?.isAffix,
            // activeMenu: data[i]?.activeMenu || "",
            activeMenu: "",
          },
          children: [],
          redirect: "",
        };
        // console.log("component", route.component);
        // if (data[i].menuType == "1") {
        //   route.redirect = `${data[i]?.redirect}` || HOME_URL;
        // }
        // 递归处理子节点
        const children = generateRoutes(data, data[i].menuId);
        if (children.length > 0) {
          route.children = children;
          route.redirect = `${data[i]?.redirect}` || HOME_URL;
        }

        routeList.push(route);
      }
    }
  }
  return routeList;
}

/**
 * 初始化动态路由[用于生成扁平化一级路由，将后端一级路由数据转化为前端router格式的一级路由]
 */
export function generateFlattenRoutes(data: menuList[]) {
  // 首先把你需要动态路由的组件地址全部获取[vue2中可以直接用拼接的方式，但是vue3中必须用这种方式]
  // let modules = import.meta.glob("@/views/**/*.vue");
  // 类型安全的动态导入处理器
  const modules = import.meta.glob("/src/views/**/*.vue") as Record<
    string,
    () => Promise<{ default: DefineComponent }>
  >;
  const routes: AppRouteRecordRaw[] = [];
  for (var i = 0; i < data.length; i++) {
    // console.log("component", data[i].component)
    const componentPath = `/src/views/${data[i].component}.vue`;
    // 处理动态组件
    const asyncComponent = modules[componentPath]
      ? () => modules[componentPath]()
      : undefined;
    const route: AppRouteRecordRaw = {
      path: `${data[i].path}`,
      name: `${data[i].name}`,
      // 这里modules[`/src/views/${componentTemplate}.vue`] 一定要用绝对定位
      component: asyncComponent || markRaw(Default as DefineComponent),
      meta: {
        parentId: data[i].parentId,
        title: data[i].menuName,
        enName: data[i]?.enName,
        icon: data[i]?.icon,
        isHide: data[i]?.isHide,
        isKeepAlive: data[i]?.isKeepAlive,
        isLink: data[i]?.isLink,
        isFull: data[i]?.isFull,
        isAffix: data[i]?.isAffix,
        // activeMenu: data[i]?.activeMenu,
        activeMenu: "",
      },
      children: [],
      redirect: "",
    };
    // console.log("component", route.component)
    if (data[i].menuType == "2") {
      route.redirect = `${data[i]?.redirect}` || HOME_URL;
    }
    routes.push(route);
  }
  return routes;
}
/**
 * @description 使用递归找出所有面包屑存储到 pinia 中
 * @param {Array} menuList 菜单列表
 * @param {Array} parent 父级菜单
 * @param {Object} result 处理后的结果
 * @returns {Object}
 */
export const getAllBreadcrumbList = (
  menuList: any,
  parent = [],
  result: { [key: string]: any } = {}
) => {
  for (const item of menuList) {
    result[item.path] = [...parent, item];

    if (item.children)
      getAllBreadcrumbList(item.children, result[item.path], result);
  }
  return result;
};
const mode = import.meta.env.VITE_ROUTER_MODE;

/**
 * @description 获取不同路由模式所对应的 url + params
 * @returns {String}
 */
export function getUrlWithParams() {
  const url = {
    hash: location.hash.substring(1),
    history: location.pathname + location.search,
  };
  // @ts-ignore
  return url[mode];
}
