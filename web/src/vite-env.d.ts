/// <reference types="vite/client" />
/// <reference types="vite-plugin-svg-icons/client" />

// SVG Icons virtual module declaration
declare module "virtual:svg-icons-register" {
  const register: string;
  export default register;
}

//./types/vue.d.ts
declare module "*.vue" {
  import type { DefineComponent } from "vue";
  const component: DefineComponent<{}, {}, any>;
  export default component;
}
