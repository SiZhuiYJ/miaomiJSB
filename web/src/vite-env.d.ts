/// <reference types="vite/client" />
/// <reference types="vite-plugin-svg-icons/client" />

// SVG Icons virtual module declaration
declare module "virtual:svg-icons-register" {
  const register: string;
  export default register;
}
import type { ViewportBindingValue } from './directives/viewport'

declare module 'vue' {
  interface ComponentCustomProperties {
    vViewport: typeof import('./directives/viewport')['viewportDirective']
  }
}

// 扩展全局指令类型
declare module '@vue/runtime-core' {
  interface GlobalDirectives {
    viewport: (value: ViewportBindingValue) => void
  }
}

// md文件预览相关类型声明
declare module "@kangc/v-md-editor/lib/preview";
declare module "@kangc/v-md-editor/lib/plugins/line-number/index";
declare module "@kangc/v-md-editor/lib/plugins/copy-code/index";
declare module "@kangc/v-md-editor/lib/plugins/emoji/index";
declare module "@kangc/v-md-editor/lib/plugins/todo-list/index";
// 添加对vuepress主题的类型声明
declare module "@kangc/v-md-editor/lib/theme/vuepress.js";
// 添加对codemirror的类型声明
declare module "codemirror";
// 添加对codemirror-editor的类型声明
declare module "@kangc/v-md-editor/lib/codemirror-editor" {
  import { DefineComponent } from "vue";

  interface VMdEditorInstance {
    [key: string]: any;
  }

  const component: VMdEditorInstance &
    DefineComponent<{}, {}, any> & { install: (app: any) => void };
  export default component;

  // 导出Codemirror属性和use方法
  export const Codemirror: any;
  export function use(theme: any, options?: any): void;
}

// prismjs
declare module "prismjs";