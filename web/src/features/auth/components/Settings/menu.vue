<script setup>
import SettingsMenu from "./SettingsMenu.vue";
import SettingsMenuItem from "./SettingsMenuItem.vue";
import router from "@/routers/index";
import { ref } from "vue";
const menuList = ref([
  { name: "profile", path: "/setting/profile" },
  { name: "account", path: "/setting/account" },
  { name: "avatar", path: "/setting/avatar" },
  { name: "deactivate", path: "/setting/deactivate" },
  { name: "password", path: "/setting/password" },
]);
const menuName = ref("");
</script>

<template>
  <SettingsMenu>
    <template #title>{{ menuName }}</template>
    <template #default="{ isCheckbox }">
      <SettingsMenuItem
        v-for="(value, index) in menuList"
        :index="index"
        :isOpen="isCheckbox"
        @click="(router.push(value.path), (menuName = value.name))"
      >
        {{ value.name }}
      </SettingsMenuItem>
    </template>
  </SettingsMenu>
</template>
<style scoped lang="scss"></style>
