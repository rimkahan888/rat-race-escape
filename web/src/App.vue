<script setup lang="ts">
import ExitMark from './components/ExitMark.vue'
import TableShell from './components/TableShell.vue'
</script>

<template>
  <div class="shell">
    <header class="top">
      <p class="mark">
        <ExitMark />
        <span>Rat Race Escape</span>
      </p>
      <p class="session">
        No game in progress
      </p>
    </header>
    <main>
      <TableShell />
    </main>
  </div>
</template>

<style scoped>
.shell {
  min-height: 100vh;
  display: grid;
  grid-template-rows: auto 1fr;
  padding: 1.4rem clamp(1.2rem, 4vw, 3rem) 2.5rem;
}

.top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
}

.mark {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  margin: 0;
  font-size: 0.95rem;
  font-weight: 500;
  letter-spacing: 0.02em;
}

.session {
  margin: 0;
  color: var(--muted);
  font-size: 0.85rem;
}

@media (max-width: 640px) {
  .top {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
