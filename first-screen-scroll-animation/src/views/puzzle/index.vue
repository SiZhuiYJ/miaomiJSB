<script setup lang="ts">
import PuzzleBoard from './components/PuzzleBoard.vue';
import PuzzleSidebar from './components/PuzzleSidebar.vue';
import PuzzleToolbar from './components/PuzzleToolbar.vue';
import { providePuzzleContext } from './context';
import { usePuzzleEditor } from './composables/usePuzzleEditor';

const puzzle = usePuzzleEditor();
providePuzzleContext(puzzle);
</script>

<template>
  <main class="puzzle-page">
    <PuzzleToolbar />
    <section class="workspace">
      <PuzzleBoard />
      <PuzzleSidebar />
    </section>
  </main>
</template>

<style lang="scss" src="./styles.scss"></style>
