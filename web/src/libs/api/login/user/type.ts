// @/libs/api/user/type.ts
import type { ToUser } from "@/stores";
export type handoverParams = "RainbowId" | "userPhone" | "userEmail";
// 登录参数
export interface ILoginParams {
  Type: handoverParams;
  Identifier: string;
  Password: string;
}
// 注册参数
export interface IRegisterParams {
  Type: string;
  Identifier: string;
  RainbowID: string;
  Password: string;
}
//  登录返回数据
export interface Data {
  jwtTokenResult: ToUser;
}
export interface UserInfo {
  rainbowId: string;
  userImg: string;
  userName: string;
}
export interface ToInfo {
  userInfo: UserInfo;
  buttonList: string[];
  roleList: string[];
}
export interface menuList {
  menuId: number; //菜单唯一标识符，用于区分不同菜单项	                            85
  menuName: string; //菜单显示的中文名称（面向用户）	                                "SVG 图标"
  enName: string; //菜单的英文名称（可能用于国际化或多语言场景）	                    "SVG Icon"
  parentId: number; //父级菜单的 ID（用于构建层级结构，0 表示根节点）	                8
  menuType: string; //菜单类型（通常用于区分目录、页面、按钮等，需结合业务约定）	        "1"=目录, "2"=页面
  path: string; //前端路由路径（对应 Vue Router 的 path）	                        "/module/svgIcon"
  name: string; //路由唯一名称（用于编程式导航，对应 Vue Router 的 name）	                "svgIconPage"
  component: string; //组件路径（动态导入时的文件路径，如 @/views/module/svgIcon/index.vue）	    "module/svgIcon/index"
  icon: string; //图标标识（可能对应 Element-UI/Ant Design 的图标组件名或自定义图标库）	    "ReadingLamp"
  isHide: string; //是否隐藏菜单（"1"=隐藏，"0"=显示；隐藏后可能仍可通过路径访问，需结合权限控制）	"1"
  isLink: string; //是否是外部链接（非空时表示跳转到外部 URL，如 "https://example.com"）	            ""
  isKeepAlive: string; //是否缓存组件（"1"=启用 keep-alive 缓存，"0"=不缓存）	                     "0"
  isFull: string; //是否全屏显示（"1"=隐藏侧边栏/导航栏，"0"=正常布局）	                    "1"
  isAffix: string; //是否固定标签页（"1"=在标签栏中不可关闭，"0"=可关闭）	                    "1"
  redirect: string; //重定向路径（为空时无重定向，常用于父级目录跳转子页面）	                     ""
}
