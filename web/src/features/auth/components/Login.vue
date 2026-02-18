<script setup lang="ts">
import { ref, onMounted } from "vue";
import { meowMsgError, meowMsgSuccess } from "@/utils/message";
import { ElLoading } from "element-plus";
import type { handoverParams } from "@/libs/api/login/user/type";
import { useUserStore } from "@/stores/";

interface handoverType {
  value: handoverParams;
  label: "账号" | "电话" | "邮箱";
}
const Identifier = ref("admin_rainbow");
const password = ref("Admin@123456");
const handover = ref<handoverParams>("RainbowId");
// 登录类型
const handoverList: handoverType[] = [
  {
    label: "账号",
    value: "RainbowId",
  },
  {
    label: "电话",
    value: "userPhone",
  },
  {
    label: "邮箱",
    value: "userEmail",
  },
];
const login = async () => {
  const loading = ElLoading.service({
    lock: true,
    text: "登录中……",
    background: "rgba(0, 0, 0, 0.7)",
  });
  useUserStore()
    .login({
      Type: handover.value,
      Identifier: Identifier.value,
      Password: password.value,
    })
    .then(() => {
      loading.close();
      meowMsgSuccess("登录成功");
    })
    .catch((err) => {
      loading.close();
      meowMsgError(err.message);
    });
};
onMounted(async () => { });
</script>

<template>
  <div class="login-box">
    <h1>--登--录--</h1>
    <el-input v-model="Identifier" style="width: 240px; padding: 15px 0"
      :placeholder="handoverList.find((item) => item.value === handover)?.label" clearable />
    <el-input v-model="password" style="width: 240px; padding: 15px 0 30px" type="password" placeholder="密码"
      show-password class="input" />
    <el-radio-group v-model="handover" @change="Identifier = ''" fill="#a262ad80" text-color="#FFF">
      <el-radio-button v-for="item in handoverList" :label="item.label" :value="item.value" :key="item.value" />
    </el-radio-group>
    <button @click="login">登录</button>
  </div>
</template>

<style scoped lang="scss">
:deep(.el-radio-button__inner):hover {
  color: #a262ad;
}

:deep(.el-input__wrapper) {
  background-color: rgba(255, 255, 255, 0);
  box-shadow: none;
}

:deep(.el-input__wrapper).is-focus {
  box-shadow: none;
}

:deep(.el-input__wrapper):hover {
  box-shadow: none;
}

h1 {
  text-align: center;
  margin-bottom: 25px;
  /* 大写 */
  text-transform: uppercase;
  color: #fff;
  /* 字间距 */
  letter-spacing: 5px;
}

input,
:deep(.el-input__inner) {
  background-color: transparent;
  width: 70%;
  color: #fff;
  border: none;
  /* 下边框样式 */
  border-bottom: 1px solid rgba(255, 255, 255, 0.4);
  padding: 10px 0;
  text-indent: 10px;
  margin: 8px 0;
  font-size: 14px;
  letter-spacing: 2px;
}

input::placeholder,
:deep(.el-input__inner)::placeholder {
  color: #fff;
}

input:focus,
:deep(.el-input__inner):focus {
  color: #a262ad;
  outline: none;
  border-bottom: 1px solid #a262ad80;
  transition: 0.5s;
}

input:focus::placeholder,
:deep(.el-input__inner):focus::placeholder {
  opacity: 0;
}

button {
  width: 70%;
  margin-top: 35px;
  background-color: #f6f6f6;
  outline: none;
  border-radius: 8px;
  padding: 13px;
  color: #a262ad;
  letter-spacing: 2px;
  border: none;
  // cursor: pointer;
}

button:hover {
  background-color: #a262ad;
  color: #f6f6f6;
  transition: background-color 0.5s ease;
}
</style>
