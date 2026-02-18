/// <reference types="vite/client" />
/// <reference types="vite-plugin-svg-icons/client" />
//./types/vue.d.ts
declare module "*.vue" {
  import type { DefineComponent } from "vue";
  const component: DefineComponent<{}, {}, any>;
  export default component;
}