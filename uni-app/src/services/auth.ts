// src/services/auth.ts
export interface WechatLoginResult {
    token: string;
    expiresIn: number;
    user: {
        id: string;
        nickname: string;
        avatar: string;
    };
}

export async function wechatLogin(): Promise<WechatLoginResult> {
    // 1. 获取微信 code
    const [err, res] = await uni.login({
        provider: 'weixin',
    });
    if (err) {
        throw new Error('微信登录失败：' + err.errMsg);
    }
    const code = res.code;

    // 2. 调用后端接口
    const response = await uni.request({
        url: '/mm/Auth/wechat/register',
        method: 'POST',
        data: { code },
        header: { 'Content-Type': 'application/json' }
    });

    if (response.statusCode !== 200) {
        throw new Error('登录失败：' + response.errMsg);
    }

    const data = response.data as WechatLoginResult;
    // 3. 存储 token 到本地
    uni.setStorageSync('token', data.token);
    return data;
}
/**
 * 微信一键登录-注册
 */
export async function wechatRegister(nickName: string,): Promise<WechatLoginResult> {
    // 1. 获取微信 code
    const [err, res] = await uni.login({
        provider: 'weixin',
    });
    if (err) {
        throw new Error('微信登录失败：' + err.errMsg);
    }
    const code = res.code;

    // 2. 调用后端接口
    const response = await uni.request({
        url: '/mm/Auth/wechat/register',
        method: 'POST',
        data: { code, nickName },
        header: { 'Content-Type': 'application/json' }
    });

    if (response.statusCode !== 200) {
        throw new Error('登录失败：' + response.errMsg);
    }

    const data = response.data as WechatLoginResult;
    // 3. 存储 token 到本地
    uni.setStorageSync('token', data.token);
    return data;
}