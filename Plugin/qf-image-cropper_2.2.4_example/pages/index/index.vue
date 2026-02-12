<template>
	<view>
		<view class="nav-item" @click="navClick('../demo/base')">基本示例</view>
		<view class="nav-item" @click="navClick('../demo/size')">自定义尺寸(1200x900)</view>
		<view class="nav-item" @click="navClick('../demo/circle')">圆形头像裁剪</view>
		<view class="nav-item" @click="navClick('../demo/round')">圆角矩形</view>
		<view class="nav-item" @click="navClick('../demo/bounce')">无回弹效果 / 不可超出边界</view>
		<view class="nav-item" @click="navClick('../demo/style')">无参考线 + 无边框</view>
		<view class="nav-item" @click="navClick('../demo/angle')">禁止拉伸裁剪框 + 禁止翻转图片</view>
		<view class="nav-item" @click="navClick('../demo/src')">展示默认图片 + 不允许从本地选择图片</view>
	</view>
</template>

<script>
	export default {
		methods: {
			navClick(url) {
				uni.navigateTo({
					url
				})
			}
 		}
	}
</script>

<style lang="scss" scoped>
	.title {
		text-align: center;
		font-weight: bold;
		line-height: 100rpx;
		background-color: #f5f5f5;
	}
	.nav-item {
		position: relative;
		line-height: 100rpx;
		padding: 0 30rpx;
		border-bottom: 1px solid #f5f5f5;
		&:after {
			position: absolute;
			top: 50%;
			z-index: 9;
			content: " ";
			height: 16rpx;
			width: 16rpx;
			border-width: 1px 1px 0 0;
			border-color: $uni-text-color-grey;
			border-style: solid;
			transform: translateY(-50%) rotate(45deg);
			right: $uni-spacing-row-lg;
			border-radius: 1px;
		}
	}

</style>
