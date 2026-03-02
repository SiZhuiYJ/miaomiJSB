<script setup>
import SettingsMenu from "./SettingsMenu.vue";
import SettingsMenuItem from "./SettingsMenuItem.vue";
import { ref } from "vue";
const menuList = ref([
  { name: "Food" },
  { name: "Entertainment" },
  { name: "Blog" },
  { name: "Location" },
]);
</script>

<template>
  <SettingsMenu v-slot="{ isCheckbox }">
    <SettingsMenuItem
      v-for="(value, index) in menuList"
      :index="index"
      :isOpen="isCheckbox"
    >
      {{ value.name }}
    </SettingsMenuItem>
  </SettingsMenu>
</template>
<style scoped lang="scss"></style>
