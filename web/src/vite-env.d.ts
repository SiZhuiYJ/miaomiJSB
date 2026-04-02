/// <reference types="vite/client" />
/// <reference types="vite-plugin-svg-icons/client" />

// SVG Icons virtual module declaration
declare module "virtual:svg-icons-register" {
  const register: string;
  export default register;
}