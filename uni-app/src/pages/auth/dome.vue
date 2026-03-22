<!-- 登录页面 -->
<template>
    <view class="cell-group">
        <wd-cell-group :border="true">
            <wd-cell title="用户头像">
                <button class="cell-wrapper" open-type="chooseAvatar" @chooseavatar="getUserProfile">
                    <wd-img width="60rpx" height="60rpx" round mode="aspectFill" :src="avatarUrl || defaultAvatarImg" />
                    <wd-icon custom-class="ml-10rpx!" name="arrow-right1" size="28rpx"></wd-icon>
                </button>
            </wd-cell>
        </wd-cell-group>
        <wd-cell-group :border="true">
            <wd-cell custom-class="nick-name" title="用户昵称" :value="nickName">
                <view class="flex justify-end items-center w380rpx">
                    <input type="nickname" maxlength="20"
                        class="cell-wrapper color-#777777 text-28rpx mr8rpx text-right" v-model="nickName"
                        placeholder="请输入用户昵称" @change="handleChange" />
                    <wd-img width="28rpx" height="28rpx" mode="aspectFill" :src="editDown" />
                </view>
            </wd-cell>
        </wd-cell-group>
    </view>
</template>

<script setup lang="ts">

// 防抖函数实现
const debounce = (func: (arg: any) => void, wait: number) => {
    let timeout: number | null = null;
    return (args: any) => {
        if (timeout) {
            clearTimeout(timeout);
        }
        timeout = setTimeout(() => {
            func(args);
        }, wait);
    };
};

/** 用户头像 */
const avatarUrl = ref('https://xxx.png')

/** 默认头像 */
const defaultAvatarImg = ref('') // 可以设置默认头像的路径

/** 获取用户头像 */
const getUserProfile = async (e: any) => {
    // 注意：这里是微信给我们返回图片的临时地址，实际开发中还需要上传图片到服务器
    avatarUrl.value = e.detail.avatarUrl
    // 业务操作：如更新头像到服务器
    console.log('操作成功...')
}

/** 用户昵称的正则表达式验证：1-20个字符 */
const nickNameExp = /^(?! *$)[\s\S]{1,20}$/

/** 用户昵称 */
const nickName = ref('')

/** 编辑图标 */
const editDown = ref('') // 另外，这里也需要一个编辑图标的路径

/** 用户昵称输入框改变 */
const handleChange = debounce((e) => {
    const nickNameInput = e.detail.value.trim()
    // if (nickNameInput === userStore.userInfo.nickName) return
    if (!nickNameExp.test(nickNameInput)) {
        console.log('用户昵称格式有误')
        return
    }
    // 业务操作：如更新昵称到服务器
    console.log('操作成功...')
}, 300)
</script>

<style scoped lang="scss">
.cell-group {
    padding: 32rpx;
    margin-top: 224rpx;

    .cell-wrapper {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        padding: 0;
        line-height: 1;
        background: none;

        &::after {
            border: none;
        }
    }

    .nick-name {
        color: #2f3032;

        :deep(.wd-cell__right) {
            flex: none !important;
        }
    }

    :deep(.wd-cell-group) {
        margin-top: 32rpx;
        overflow: hidden;
        border: 2rpx solid #d9e1e9;
        border-radius: 20rpx;
    }

    :deep(.wd-cell__wrapper) {
        align-items: center;
        height: 96rpx;
        padding: 28rpx 0;
    }

    :deep(.wd-cell__right) {
        padding-right: 32rpx;
    }

    :deep(.wd-cell__title) {
        --wot-cell-title-fs: 28rpx;
        color: #2f3032;
    }

    :deep(.wd-cell__value) {
        --wot-cell-value-fs: 27rpx;

        color: rgb(0 0 0 / 60%);
    }

    :deep(.wd-cell__arrow-right) {
        --wot-cell-arrow-size: 30rpx;

        color: rgb(0 0 0 / 60%);
    }
}
</style>