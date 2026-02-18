<script setup lang="ts">
import { ref } from "vue";
import { meowMsgError, meowMsgSuccess } from "@/utils/message";
import { ElLoading } from "element-plus";
import { userApi } from "@/libs/api/login/user";
const rainbowID = ref("");
//手机
type handoverParams = "userPhone" | "userEmail";
interface handoverType {
  value: handoverParams;
  label: "电话" | "邮箱";
}
const handover = ref<handoverParams>("userPhone");
// 登录类型
const handoverList: handoverType[] = [
  {
    label: "电话",
    value: "userPhone",
  },
  {
    label: "邮箱",
    value: "userEmail",
  },
];
const userPhone = ref("");
const userEmail = ref("");
const password = ref("");
const confirm_password = ref("");
const register = async () => {
  const loading = ElLoading.service({
    lock: true,
    text: "Loading",
    background: "rgba(0, 0, 0, 0.7)",
  });
  try {
    const data = await userApi.PostRegister({
      Type: handover.value,
      Identifier: handover.value === "userPhone" ? userPhone.value : userEmail.value,
      RainbowID: rainbowID.value,
      Password: password.value,
    });
    console.log(data);
    loading.close();
    meowMsgSuccess("注册成功");
  } catch (error) {
    loading.close();
    meowMsgError("注册失败");
  }
};

function handoverLogin() {
  userEmail.value = "";
}
</script>

<template>
  <div class="register-box">
    <h1>--注--册--</h1>
    <el-input class="rainbow" v-model="rainbowID" style="width: 240px; padding: 15px 0" placeholder="账号" clearable />
    <el-input class="phone" v-show="handover === 'userPhone'" v-model="userPhone" style="width: 240px; padding: 9px 0"
      placeholder="电话" clearable />
    <el-input class="email" v-show="handover === 'userEmail'" v-model="userEmail" style="width: 240px; padding: 9px 0"
      placeholder="邮箱" clearable />
    <el-input class="input" v-model="password" style="width: 240px; padding: 9px 0" type="password" placeholder="密码"
      show-password />
    <el-input class="input confirmation" v-model="confirm_password" style="width: 240px; padding: 9px 0" type="password"
      placeholder="确认密码" show-password />
    <el-radio-group v-model="handover" @change="handoverLogin" fill="#a262ad80" text-color="#FFF">
      <el-radio-button v-for="(item, index) in handoverList" :key="index" :label="item.label" :value="item.value" />>
    </el-radio-group>
    <button @click="register">注册</button>
  </div>
</template>

<style lang="scss" scoped>
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
