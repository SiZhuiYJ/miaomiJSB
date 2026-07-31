<script setup lang="ts">
import { RouterLink } from 'vue-router'

import { ref, onMounted } from 'vue';

const userAgent = ref('');
const deviceType = ref('未知');

onMounted(() => {
  // 获取 User-Agent 字符串
  userAgent.value = navigator.userAgent;

  // 一个简单的设备类型判断示例
  const ua = userAgent.value.toLowerCase();
  if (/mobile|android|iphone|ipad|ipod/.test(ua)) {
    deviceType.value = '移动设备';
  } else {
    deviceType.value = '桌面设备';
  }
});

const manualSections = [
  {
    title: '基础写法',
    items: [
      { name: '变量', syntax: 'x', example: 'x' },
      { name: '四则运算', syntax: '+、-、*、/、%', example: '2x + 1' },
      { name: '括号', syntax: '(...)', example: '(x + 2) / 3' },
      { name: '平方', syntax: 'x^2', example: 'x^2 + 2x + 1' },
    ],
  },
  {
    title: '根号和次方根',
    items: [
      { name: '平方根', syntax: 'sqrt(x) 或 √(x)', example: 'sqrt(25 - x^2)' },
      { name: '立方根', syntax: 'cbrt(x)', example: 'cbrt(x - 1)' },
      { name: 'n 次方根', syntax: 'x^(1/n)', example: '(x + 2)^(1/4)' },
      { name: 'pow 写法', syntax: 'pow(x, 1/n)', example: 'pow(x + 2, 1/3)' },
    ],
  },
  {
    title: '常用函数',
    items: [
      { name: '三角函数', syntax: 'sin(x)、cos(x)、tan(x)', example: 'sin(x) + cos(2x)' },
      { name: '反三角函数', syntax: 'asin(x)、acos(x)、atan(x)', example: 'atan(x)' },
      { name: '绝对值', syntax: 'abs(x)', example: 'abs(x) - 2' },
      { name: '指数函数', syntax: 'exp(x)', example: 'exp(0.2x)' },
      { name: '对数', syntax: 'ln(x)、log(x)、lg(x)', example: 'ln(x + 3)' },
    ],
  },
  {
    title: '常量',
    items: [
      { name: '圆周率', syntax: 'pi 或 π', example: 'sin(pi * x)' },
      { name: '自然常数', syntax: 'e', example: 'e^x' },
    ],
  },
]

function formulaLink(example: string) {
  return {
    path: '/',
    query: {
      formula: example,
    },
  }
}
</script>

<template>
  <main class="manual-page">
    <header class="manual-header">
      <div>
        <p>Function Plotter</p>
        <h1>使用手册</h1>
      </div>
      <RouterLink class="back-link" to="/">返回坐标系</RouterLink>
    </header>

    <section class="summary">
      <strong>输入规则</strong>
      <p>输入框默认表示 y = f(x)。可以直接写 x、函数名、括号和运算符，系统会实时绘制函数图像。</p>
    </section>

    <section class="summary">
      <strong>曲线颜色</strong>
      <p>坐标系右上角的“颜色”按钮可以打开颜色菜单。首页支持单色、整体渐变和线条渐变；渐变模式可以选择左右、上下和对角方向，也可以一键交换起点色和终点色。</p>
    </section>

    <section class="summary">
      <strong>坐标操作</strong>
      <p>鼠标滚轮可以以当前指针位置为中心放大或缩小坐标系，缩放时会保持 X/Y 单位在画布上的像素比例一致。按住坐标系拖动可以平移视图，左侧坐标范围会同步更新。</p>
    </section>

    <section class="manual-grid" aria-label="函数语法列表">
      <article v-for="section in manualSections" :key="section.title" class="manual-section">
        <h2>{{ section.title }}</h2>

        <div class="manual-list">
          <div v-for="item in section.items" :key="item.name" class="manual-row">
            <strong>{{ item.name }}</strong>
            <code>{{ item.syntax }}</code>
            <RouterLink :to="formulaLink(item.example)">套用 {{ item.example }}</RouterLink>
          </div>
        </div>
      </article>
    </section>

    <section class="note">
      <strong>注意</strong>
      <p>根号内容建议始终加括号，例如写成 sqrt(x + 1) 或 √(x + 1)。负数立方根请使用 cbrt(x)，普通分数指数在负数区域可能无定义。</p>
    </section>

    <div>
      <p>当前设备类型: {{ deviceType }}</p>
      <p>User-Agent: {{ userAgent }}</p>
    </div>

  </main>
</template>

<style scoped>
.manual-page {
  min-height: 100vh;
  display: grid;
  align-content: start;
  gap: 22px;
  padding: 28px;
  background:
    linear-gradient(180deg, rgba(15, 118, 110, 0.08), rgba(248, 250, 252, 0) 42%),
    #eef4f7;
  color: #111827;
}

.manual-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;
}

.manual-header p {
  margin: 0 0 4px;
  color: #64748b;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0;
  text-transform: uppercase;
}

.manual-header h1 {
  margin: 0;
  color: #0f172a;
  font-size: 36px;
  line-height: 1.1;
}

.back-link,
.manual-row a {
  min-height: 36px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  text-decoration: none;
  font-weight: 800;
}

.back-link {
  border: 1px solid #cbd5e1;
  padding: 0 14px;
  background: #ffffff;
  color: #0f766e;
}

.back-link:hover,
.manual-row a:hover {
  border-color: #0f766e;
  background: #ecfeff;
}

.summary,
.note,
.manual-section {
  border: 1px solid #d7e0e8;
  border-radius: 8px;
  background: #ffffff;
  box-shadow: 0 18px 40px rgba(15, 23, 42, 0.08);
}

.summary,
.note {
  padding: 18px 20px;
}

.summary strong,
.note strong {
  color: #0f172a;
  font-size: 16px;
}

.summary p,
.note p {
  margin: 8px 0 0;
  color: #475569;
  line-height: 1.7;
}

.manual-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 18px;
}

.manual-section {
  overflow: hidden;
}

.manual-section h2 {
  margin: 0;
  border-bottom: 1px solid #e2e8f0;
  padding: 16px 18px;
  color: #0f172a;
  font-size: 20px;
  line-height: 1.2;
}

.manual-list {
  display: grid;
}

.manual-row {
  display: grid;
  grid-template-columns: 90px minmax(0, 1fr) auto;
  gap: 12px;
  align-items: center;
  padding: 14px 18px;
}

.manual-row+.manual-row {
  border-top: 1px solid #edf2f7;
}

.manual-row strong {
  color: #334155;
  font-size: 14px;
}

.manual-row code {
  min-width: 0;
  color: #0f766e;
  font-family: Consolas, "Courier New", monospace;
  font-size: 14px;
  overflow-wrap: anywhere;
}

.manual-row a {
  border: 1px solid #cbd5e1;
  padding: 0 10px;
  background: #f8fafc;
  color: #075985;
  font-family: Consolas, "Courier New", monospace;
  font-size: 12px;
  overflow-wrap: anywhere;
}

@media (max-width: 980px) {
  .manual-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 640px) {
  .manual-page {
    padding: 14px;
  }

  .manual-header {
    display: grid;
  }

  .manual-header h1 {
    font-size: 30px;
  }

  .back-link {
    justify-self: start;
  }

  .manual-row {
    grid-template-columns: 1fr;
  }

  .manual-row a {
    justify-self: start;
  }
}
</style>
