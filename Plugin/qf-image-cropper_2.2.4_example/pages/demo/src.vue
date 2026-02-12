<template>
	<view>
		<qf-image-cropper :choosable="false" src="https://ask.dcloud.net.cn/uploads/avatar/001/67/43/81_avatar_max.jpg" @crop="handleCrop"></qf-image-cropper>
	</view>
</template>

<script>
	import QfImageCropper from '@/uni_modules/qf-image-cropper/components/qf-image-cropper/qf-image-cropper.vue';
	export default {
		components: {
			QfImageCropper
		},
		methods: {
			handleCrop(e) {
				uni.previewImage({
					urls: [e.tempFilePath],
					current: 0
				});
			}
 		}
	}
</script>

<style>

</style>
