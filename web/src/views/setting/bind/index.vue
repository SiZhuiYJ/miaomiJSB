<script setup lang="ts">
import type {  ThirdPartyBinding } from "@/features/auth/types";
import { ref, onMounted, watch } from 'vue';
import { useAuthStore } from '@/stores';
import { storeToRefs } from 'pinia'
import { authApi } from '@/features/auth/api';
import { notifySuccess, notifyError } from '@/utils/notification';
import { toLocalDateOnlyString, parseDateOnly } from "@/utils/date";

const authStore = useAuthStore();
const { user } = storeToRefs(authStore);

interface thirdBinds {
    name: string;// 中文名
    provider: string;// 对应编号
    icon: string;// 图标路径
    toggleBind: (bindName: string) => void;// 绑定/解绑函数
}

const loading = ref(false);

// 绑定第三方映数组
const thirdPartyBindings = ref<thirdBinds[]>([
    {
        name: '微信',
        provider: 'wechat',
        icon: '/assets/icons/appIco/weixin.svg',
        toggleBind: toggleWechatBind
    }, {
        name: '百度',
        provider: 'baidu',
        icon: '/assets/icons/appIco/baidu.svg',
        toggleBind: toggleBind
    },
    {
        name: '爱奇艺',
        provider: 'aiqiyi',
        icon: '/assets/icons/appIco/aiqiyi.svg',
        toggleBind: toggleBind
    },
    {
        name: '百度贴吧',
        provider: 'baidutieba',
        icon: '/assets/icons/appIco/baidutieba.svg',
        toggleBind: toggleBind
    },
    {
        name: '哔哩哔哩',
        provider: 'bilibili',
        icon: '/assets/icons/appIco/bilibili.svg',
        toggleBind: toggleBind
    },
    {
        name: '钉钉',
        provider: 'dingding',
        icon: '/assets/icons/appIco/dingding.svg',
        toggleBind: toggleBind
    },
    {
        name: '豆瓣网站',
        provider: 'doubanwang',
        icon: '/assets/icons/appIco/doubanwang.svg',
        toggleBind: toggleBind
    },
    {
        name: '饿了么',
        provider: 'elemo',
        icon: '/assets/icons/appIco/elemo.svg',
        toggleBind: toggleBind
    },
    {
        name: 'Facebook',
        provider: 'facebook',
        icon: '/assets/icons/appIco/facebook.svg',
        toggleBind: toggleBind
    },
    {
        name: '公众号',
        provider: 'gongzhonghao',
        icon: '/assets/icons/appIco/gongzhonghao.svg',
        toggleBind: toggleBind
    },
    {
        name: '谷歌',
        provider: 'google',
        icon: '/assets/icons/appIco/google.svg',
        toggleBind: toggleBind
    },
    {
        name: '花瓣网',
        provider: 'huabanwang',
        icon: '/assets/icons/appIco/huabanwang.svg',
        toggleBind: toggleBind
    },
    {
        name: '快手',
        provider: 'kuaishou',
        icon: '/assets/icons/appIco/kuaishou.svg',
        toggleBind: toggleBind
    },
    {
        name: '酷狗音乐',
        provider: 'kugouyinle',
        icon: '/assets/icons/appIco/kugouyinle.svg',
        toggleBind: toggleBind
    },
    {
        name: 'LinkedIn',
        provider: 'linkedin',
        icon: '/assets/icons/appIco/linkedin.svg',
        toggleBind: toggleBind
    },
    {
        name: '美团',
        provider: 'meituan',
        icon: '/assets/icons/appIco/meituan.svg',
        toggleBind: toggleBind
    },
    {
        name: '陌陌',
        provider: 'momo',
        icon: '/assets/icons/appIco/momo.svg',
        toggleBind: toggleBind
    },
    {
        name: '企业微信',
        provider: 'qiyeweixin',
        icon: '/assets/icons/appIco/qiyeweixin.svg',
        toggleBind: toggleBind
    },
    {
        name: 'QQ音乐',
        provider: 'QQyinle',
        icon: '/assets/icons/appIco/QQyinle.svg',
        toggleBind: toggleBind
    },
    {
        name: '淘宝',
        provider: 'taobao',
        icon: '/assets/icons/appIco/taobao.svg',
        toggleBind: toggleBind
    },
    {
        name: '腾讯会议',
        provider: 'tengxunhuiyi',
        icon: '/assets/icons/appIco/tengxunhuiyi.svg',
        toggleBind: toggleBind
    },
    {
        name: '腾讯QQ',
        provider: 'tengxunQQ',
        icon: '/assets/icons/appIco/tengxunQQ.svg',
        toggleBind: toggleBind
    },
    {
        name: '腾讯视频',
        provider: 'tengxunshipin',
        icon: '/assets/icons/appIco/tengxunshipin.svg',
        toggleBind: toggleBind
    },
    {
        name: '腾讯微视',
        provider: 'tengxunweishi',
        icon: '/assets/icons/appIco/tengxunweishi.svg',
        toggleBind: toggleBind
    },
    {
        name: '推特',
        provider: 'twitter',
        icon: '/assets/icons/appIco/twitter.svg',
        toggleBind: toggleBind
    },
    {
        name: '网易云音乐',
        provider: 'wangyiyunyinle',
        icon: '/assets/icons/appIco/wangyiyunyinle.svg',
        toggleBind: toggleBind
    },
    {
        name: '小红书',
        provider: 'xiaohongshu',
        icon: '/assets/icons/appIco/xiaohongshu.svg',
        toggleBind: toggleBind
    },
    {
        name: '新浪微博',
        provider: 'xinlang',
        icon: '/assets/icons/appIco/xinlang.svg',
        toggleBind: toggleBind
    },
    {
        name: 'YouTube',
        provider: 'youtube',
        icon: '/assets/icons/appIco/youtube.svg',
        toggleBind: toggleBind
    },
    {
        name: '支付宝',
        provider: 'zhifubao',
        icon: '/assets/icons/appIco/zhifubao.svg',
        toggleBind: toggleBind
    },
    {
        name: '知乎',
        provider: 'zhihu',
        icon: '/assets/icons/appIco/zhihu.svg',
        toggleBind: toggleBind
    },

]);

watch(() => user.value?.thirdPartyBindings, (newBindings) => {
    if (newBindings) {
        // 当用户信息中的第三方绑定数据发生变化时，更新本地的绑定状态
        // fetchThirdPartyBindings();
    }
}, { deep: true });

onMounted(async () => {
    // 获取最新的第三方绑定信息
    await fetchThirdPartyBindings();
});

// 获取第三方绑定信息
async function fetchThirdPartyBindings() {
    loading.value = true;
    try {
        const { data } = await authApi.getThirdPartyBindings();
        if (data) {
            // 更新用户信息中的第三方绑定数据
            authStore.updateUser({
                thirdPartyBindings: data
            });
        }
    } catch (error) {
        notifyError('获取第三方绑定信息失败');
        console.error('Failed to fetch third party bindings', error);
    } finally {
        loading.value = false;
    }
}

// 检查特定提供商是否已绑定
function hasProviderBind(provider: string) {
    const bindings = user.value?.thirdPartyBindings;

    // 确保bindings是数组后再调用some方法
    if (Array.isArray(bindings)) {
        return bindings.some((binding: ThirdPartyBinding) => binding.provider === provider);
    }
    return false;
}

// 获取特定提供商的绑定日期
function getBindingDate(provider: string) {
    const bindings = user.value?.thirdPartyBindings;
    // 确保bindings是数组后再调用find方法
    if (Array.isArray(bindings)) {
        const binding = bindings.find((binding: ThirdPartyBinding) => binding.provider === provider);
        return binding ? binding.boundAt : null;
    }
    return null;
}

// 处理微信绑定/解绑
async function toggleWechatBind() {
    // if (hasProviderBind('wechat')) {
    //     // 解绑微信
    //     uni.showModal({
    //         title: '确认解绑',
    //         content: '解绑后将无法使用微信一键登录，确定要解绑吗？',
    //         confirmText: '确定解绑',
    //         cancelText: '取消',
    //         success: async (modalRes) => {
    //             if (modalRes.confirm) {
    //                 try {
    //                     await http.delete('/mm/Auth/wechat/unbind');
    //                     notifySuccess('微信解绑成功');

    //                     // 更新用户信息
    //                     await authStore.fetchUserInfo();
    //                 } catch (error: any) {
    //                     console.error('解绑微信失败:', error);

    //                     if (error.statusCode === 401) {
    //                         notifyError('用户未登录或登录已过期');
    //                     } else if (error.statusCode === 404) {
    //                         notifyError('当前用户未绑定微信账号');
    //                     } else {
    //                         notifyError(error.data?.message || '解绑失败，请稍后重试');
    //                     }
    //                 }
    //             }
    //         }
    //     });
    // } else {
    //     // 绑定微信
    //     if (hasWechatEnv()) {
    //         await bindWechat();
    //     } else {
    //         notifyError('请在微信小程序环境中使用此功能');
    //     }
    // }
    // 更新绑定信息
    fetchThirdPartyBindings();
}

// 绑定微信
// async function bindWechat() {
//     try {
//         // #ifdef MP-WEIXIN
//         const loginRes: any = await new Promise((resolve, reject) => {
//             uni.login({
//                 provider: 'weixin',
//                 success: resolve,
//                 fail: reject
//             });
//         });

//         if (!loginRes.code) {
//             throw new Error('获取微信登录凭证失败');
//         }

//         await http.post('/mm/Auth/wechat/bind', {
//             code: loginRes.code
//         });

//         notifySuccess('微信绑定成功');

//         // 更新用户信息
//         await authStore.fetchUserInfo();
//         // #endif

//         // #ifndef MP-WEIXIN
//         notifyError('请在微信小程序环境中使用此功能');
//         // #endif
//     } catch (error: any) {
//         console.error('绑定微信失败:', error);

//         if (error.statusCode === 409) {
//             notifyError(error.data?.message || '绑定失败：微信账号已被绑定');
//         } else if (error.statusCode === 401) {
//             notifyError('用户未登录或登录已过期，请重新登录');
//         } else {
//             notifyError(error.data?.message || '绑定失败，请稍后重试');
//         }
//     }
// }
// 处理百度绑定/解绑
async function toggleBind(bindName: string) {
    console.log(`${bindName}绑定/解绑功能待实现`);
    notifySuccess(`${bindName}绑定功能待实现`);
    if (hasProviderBind(bindName)) {

    }
}
</script>
<template>
    <div class="page-container">

        <div class="page-card">
            <div class="form-field">
                <text class="label">第三方账号绑定</text>
                <div class="desc">管理您绑定的第三方账号，用于便捷登录</div>
            </div>

            <div class="bind-list" v-if="!loading">
                <div class="bind-item" v-for="binding in thirdPartyBindings" :key="binding.provider">
                    <div class="bind-info">
                        <image class="bind-icon" :src="binding.icon" mode="aspectFit" />
                        <div class="bind-details">
                            <text class="bind-name">{{ binding.name }}</text>
                            <text class="bind-desc">
                                {{ hasProviderBind(binding.provider) ? '已绑定，' +
                                    toLocalDateOnlyString(parseDateOnly(getBindingDate(binding.provider)!))
                                    : '未绑定' }}
                            </text>
                        </div>
                    </div>
                    <button class="bind-btn"
                        :class="hasProviderBind(binding.provider) ? 'unbind-btn' : 'bind-btn-primary'"
                        @click="binding.toggleBind(binding.name)">
                        {{ hasProviderBind(binding.provider) ? '解绑' : '绑定' }}
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>



<style scoped lang="scss">
.bind-list {
    margin-top: 20px;
}

.bind-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 16px 0;
    border-bottom: 0.5px solid rgba(0, 0, 0, 0.1);

    &:last-child {
        border-bottom: none;
    }
}

.bind-info {
    display: flex;
    align-items: center;
    flex: 1;
}

.bind-icon {
    width: 40px;
    height: 40px;
    margin-right: 12px;
    border-radius: 8px;
}

.bind-details {
    flex: 1;
}

.bind-name {
    display: block;
    font-size: 16px;
    color: var(--text-color);
    margin-bottom: 4px;
}

.bind-desc {
    display: block;
    font-size: 14px;
    color: var(--text-muted);
}

.bind-btn {
    padding: 8px 16px;
    border-radius: 8px;
    font-size: 14px;
    border: none;
    margin-left: 16px;
}

.bind-btn-primary {
    background-color: var(--theme-primary);
    color: white;
}

.unbind-btn {
    background-color: #f0f0f0;
    color: #ff4757;
}

.desc {
    font-size: 14px;
    color: var(--text-muted);
    margin-top: 8px;
}
</style>
