<script setup lang="ts">
defineProps<{
  faces: number[]
  canRoll: boolean
  twoDice: boolean
  canEnd: boolean
  busy: boolean
}>()

const emit = defineEmits<{
  roll: [count: number]
  end: []
}>()

function faceSrc(face: number): string {
  const safe = Math.min(6, Math.max(1, face))
  return `/art/die-${safe}.webp`
}
</script>

<template>
  <section
    class="tray glass"
    aria-label="Dice"
  >
    <div class="faces">
      <img
        v-for="(face, index) in faces.length ? faces : [1]"
        :key="index"
        :src="faceSrc(face)"
        alt=""
      >
    </div>
    <div class="actions">
      <button
        type="button"
        class="gold"
        :disabled="!canRoll || busy"
        @click="emit('roll', 1)"
      >
        Roll
      </button>
      <button
        v-if="twoDice"
        type="button"
        class="ghost"
        :disabled="!canRoll || busy"
        @click="emit('roll', 2)"
      >
        Two dice
      </button>
      <button
        type="button"
        class="end"
        :disabled="!canEnd || busy"
        @click="emit('end')"
      >
        End turn
      </button>
    </div>
  </section>
</template>

<style scoped>
.tray {
  display: grid;
  gap: 0.7rem;
  padding: 0.8rem;
}

.faces {
  display: flex;
  gap: 0.4rem;
  min-height: 3.2rem;
}

.faces img {
  width: 3.1rem;
  height: auto;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

button {
  border-radius: 999px;
  border: 0;
  padding: 0.55rem 0.9rem;
  cursor: pointer;
}

button:disabled {
  opacity: 0.45;
  cursor: default;
}

.gold,
.end {
  background: linear-gradient(180deg, #f0d48a, #c4923a);
  color: #24180a;
  font-weight: 650;
}

.ghost {
  background: transparent;
  color: var(--paper);
  border: 1px solid rgba(239, 231, 214, 0.2);
}
</style>
