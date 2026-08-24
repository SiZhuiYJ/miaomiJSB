<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import router from './router'
</script>

<template>
  <header class="navigation-bar">
    <div class="wrapper">
      <nav>
        <RouterLink v-for="value in router.getRoutes()" :key="value.name" :to="value.path">
          {{ value.name }}
        </RouterLink>
      </nav>
    </div>
  </header>

  <RouterView />
</template>

<style scoped>
.navigation-bar {
  position: fixed;
  top: 0;
  margin: 0 auto;
  width: 100%;
  max-height: 100vh;

  z-index: 1000;
}

.wrapper {
  width: 100%;
  margin: 0 auto;
  text-align: center;
  display: flex;
  justify-content: center;
}



nav {
  font-size: 12px;
  background-color: #fff;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  border-radius: 0 0 4px 4px;
}

nav a.router-link-exact-active {
  color: var(--color-text);
}

nav a.router-link-exact-active:hover {
  background-color: transparent;
}

nav a {

  display: inline-block;
  padding: 0 1rem;
  border-left: 1px solid var(--color-border);
}

nav a:first-of-type {
  border: 0;
}

@media (min-width: 1024px) {
  header {
    display: flex;
    place-items: center;
  }

  header .wrapper {
    display: flex;
    place-items: flex-start;
    flex-wrap: wrap;
  }

  nav {
    text-align: left;
    font-size: 1rem;
  }
}
</style>
