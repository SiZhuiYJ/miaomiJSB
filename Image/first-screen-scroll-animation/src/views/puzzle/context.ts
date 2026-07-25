import { inject, provide } from 'vue';
import type { InjectionKey } from 'vue';
import type { usePuzzleEditor } from './composables/usePuzzleEditor';

export type PuzzleEditorContext = ReturnType<typeof usePuzzleEditor>;

const puzzleContextKey: InjectionKey<PuzzleEditorContext> = Symbol('PuzzleEditorContext');

export const providePuzzleContext = (context: PuzzleEditorContext) => {
  provide(puzzleContextKey, context);
};

export const usePuzzleContext = () => {
  const context = inject(puzzleContextKey);
  if (!context) throw new Error('Puzzle context is not provided');
  return context;
};
