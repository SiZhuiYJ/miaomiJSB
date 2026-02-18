import { useUserStore, useAuthStore } from "@/stores/";
import { LOGIN_URL } from "@/config/index.ts";
// TS OR JS 中不能直接导入 import { useRouter } from "vue-router";
import router from "@/routers/index";
import type { RouterItem } from "@/routers/type";

// const modules = import.meta.glob("@/views/**/*.vue");

export const initDynamicRouter = async () => {
  const authStore = useAuthStore();

  try {
    // 1、获取菜单列表 && 按钮权限列表 && 递归菜单数据
    await useAuthStore().listRouters();
    await useAuthStore().getLoginUserInfo();

    // 2、判断当前用户是否拥有菜单权限
    // Proxy对象转换为正常的JSON数据
    // const menuRouters = JSON.parse(JSON.stringify(authStore.menuList));
    if (authStore.menuList == null || authStore.menuList.length == 0) {
      console.log("没路由数据了", authStore.menuList);
      useUserStore().setToken(null);
      router.replace(LOGIN_URL);
      return;
    }

    // 3、添加动态路由[扁平化一级路由数据]
    // authStore.menuList.forEach((item: RouterItemID) => {
    authStore.recursiveMenuList.forEach((item: RouterItem) => {
      // if (item.component && typeof item.component == "string") {
      //   // 扁平化路由也需要构造component路由函数
      //   item.component = modules["/src/views/" + item.component + ".vue"];
      // }
      // if (item.isFull == "0") {
      //   // 如果是全屏的话，直接为整个页面
      //   router.addRoute(item);
      // } else {
      //   router.addRoute("layout", item);
      // }
      router.addRoute(item);
    });
  } catch (error) {
    console.log(error);
    // 当菜单请求出错时，重定向到登陆页
    useUserStore().setToken(null);
    router.replace(LOGIN_URL);
    return Promise.reject(error);
  }
};
