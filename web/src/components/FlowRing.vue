<script setup lang="ts">
import { computed } from 'vue'
import { formatMoney } from '../money'

const props = defineProps<{
  passiveCents: number
  expenseCents: number
  cashFlowCents: number
}>()

const ratio = computed(() => {
  if (props.expenseCents <= 0) return props.passiveCents > 0 ? 1 : 0
  return Math.min(1, Math.max(0, props.passiveCents / props.expenseCents))
})

const dash = computed(() => {
  const length = 2 * Math.PI * 36
  return `${length * ratio.value} ${length}`
})
</script>

<template>
  <div
    class="ring"
    :aria-label="`Escape progress ${Math.round(ratio * 100)} percent`"
  >
    <svg
      viewBox="0 0 88 88"
      aria-hidden="true"
    >
      <circle
        cx="44"
        cy="44"
        r="36"
        class="track"
      />
      <circle
        cx="44"
        cy="44"
        r="36"
        class="value"
        :stroke-dasharray="dash"
      />
    </svg>
    <p>
      <strong>{{ formatMoney(cashFlowCents) }}</strong>
      <span>cash flow</span>
    </p>
  </div>
</template>

<style scoped>
.ring {
  position: relative;
  width: 5.6rem;
  height: 5.6rem;
}

svg {
  width: 100%;
  height: 100%;
  transform: rotate(-90deg);
}

circle {
  fill: none;
  stroke-width: 7;
}

.track {
  stroke: rgba(239, 231, 214, 0.12);
}

.value {
  stroke: var(--green);
  stroke-linecap: round;
}

p {
  position: absolute;
  inset: 0;
  margin: 0;
  display: grid;
  place-content: center;
  text-align: center;
}

strong {
  font-size: 0.72rem;
}

span {
  color: var(--muted);
  font-size: 0.62rem;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}
</style>
