<script setup>
import SettingsMenu from "@/components/Menu/SettingsMenu.vue";
import SettingsMenuItem from "@/components/Menu/SettingsMenuItem.vue";
import router from "@/routers/index";
import { ref } from "vue";
const menuList = ref([
    { name: "个人信息", path: "/setting/profile" },
    { name: "账号修改", path: "/setting/account" },
    { name: "头像修改", path: "/setting/avatar" },
    { name: "注销账号", path: "/setting/deactivate" },
    { name: "密码修改", path: "/setting/password" },
]);
// 默认显示第一个菜单名称
const menuName = ref(menuList.value[0]?.name ?? "");
</script>

<template>
    <SettingsMenu>
        <template #title>{{ menuName }}</template>
        <template #default="{ isOpen }">
            <SettingsMenuItem v-for="(value, index) in menuList" :key="value.path" :index="index" :isOpen="isOpen"
                @click="(router.push(value.path), (menuName = value.name))">
                {{ value.name }}
            </SettingsMenuItem>
        </template>
    </SettingsMenu>
</template>
<style scoped lang="scss"></style>
