<template>
	<view>
		<qf-image-cropper ref="qfImageCropper" :width="500" :height="500" @crop="handleCrop"></qf-image-cropper>
	</view>
</template>

<script>
	import QfImageCropper from '@/uni_modules/qf-image-cropper/components/qf-image-cropper/qf-image-cropper.vue';
	export default {
		components: {
			QfImageCropper
		},
		// mounted() {
		// 	// 通过ref组件实例可在进入页面后直接打开相册选择图片
		// 	this.$refs.qfImageCropper.chooseImage({ sourceType: ['album'] });
		// },
		methods: {
			handleCrop(e) {
				uni.previewImage({
					urls: [e.tempFilePath],
					current: 0
				});
				// console.log(e)
				// // #ifdef H5
				// h5下载图片
				// var a = document.createElement('a');
				// a.href = e.tempFilePath;
				// a.download = new Date().valueOf() + ".png";
				// a.click();
				// // #endif
				// // #ifndef H5
				// 非h5保存相册
				// uni.saveImageToPhotosAlbum({
				// 	filePath: e.tempFilePath,
				// 	success: () => {
				// 		uni.showToast({
				// 			title: '保存成功',
				// 			icon: 'success'
				// 		})
				// 	}
				// });
				// // #endif
			}
 		}
	}
</script>

<style>

</style>
