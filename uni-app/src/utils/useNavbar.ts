import { ref } from 'vue';

export function useNavbar() {
  const paddingTop = ref(0);
  const height = ref(0);
  const paddingLeft = ref(0); // For right alignment to avoid overlapping with capsule

  // Initialize
  const sysInfo = uni.getSystemInfoSync();
  const statusBarHeight = sysInfo.statusBarHeight || 0;
  
  // Default values
  paddingTop.value = statusBarHeight + 4; 
  height.value = 32; // Standard capsule height
  paddingLeft.value = 16;

  // #ifdef MP-WEIXIN
  try {
    const menuButton = uni.getMenuButtonBoundingClientRect();
    paddingTop.value = menuButton.top;
    height.value = menuButton.height;
    
    // Calculate right padding/margin to avoid overlap if we have long text
    // sysInfo.windowWidth - menuButton.right gives the right margin
    // We might want similar left margin for balance, or just enough to align
    paddingLeft.value = sysInfo.windowWidth - menuButton.right;
  } catch (e) {
    console.error('getMenuButtonBoundingClientRect failed', e);
  }
  // #endif

  return {
    paddingTop,
    height,
    paddingLeft
  };
}
