<script setup lang="ts">
import { computed, ref } from 'vue'
import { spaceIcon } from '../catalog'
import { formatFlow, formatMoney } from '../money'
import type { DrawnCard } from '../types'

const props = defineProps<{
  card: DrawnCard
  busy: boolean
  yours: boolean
}>()

const emit = defineEmits<{
  buy: [shares: number | null]
  pass: []
  choice: [choice: string]
}>()

const shares = ref(1)
const isStock = computed(() => props.card.kind === 'Stock')
const icon = computed(() => {
  if (props.card.deck === 'Doodad') return '/art/bag.webp'
  if (props.card.deck === 'Market') return '/art/coins.webp'
  if (props.card.kind === 'Property') return '/art/apartment.webp'
  if (props.card.kind === 'Business') return '/art/office.webp'
  return spaceIcon('Deal', 'RatRace')
})
</script>

<template>
  <div
    class="shade"
    role="presentation"
  >
    <article
      class="card glass"
      role="dialog"
      aria-modal="true"
      :aria-label="card.title"
    >
      <img
        :src="icon"
        alt=""
      >
      <p class="kind">
        {{ card.deck }}
      </p>
      <h2>{{ card.title }}</h2>
      <p class="summary">
        {{ card.summary }}
      </p>
      <ul>
        <li v-if="card.costCents != null">
          {{ formatMoney(card.costCents) }}
        </li>
        <li v-if="card.downPaymentCents != null">
          Down {{ formatMoney(card.downPaymentCents) }}
        </li>
        <li v-if="card.pricePerShareCents != null">
          {{ formatMoney(card.pricePerShareCents) }} a share
        </li>
        <li v-if="card.monthlyCashFlowCents != null">
          {{ formatFlow(card.monthlyCashFlowCents) }} / month
        </li>
        <li v-if="card.priceCents != null">
          Offer {{ formatMoney(card.priceCents) }}
        </li>
        <li v-if="card.cashDeltaCents != null">
          {{ formatFlow(card.cashDeltaCents) }}
        </li>
      </ul>
      <div
        v-if="yours"
        class="actions"
      >
        <label v-if="isStock">
          Shares
          <input
            v-model.number="shares"
            type="number"
            min="1"
            max="9999"
          >
        </label>
        <button
          v-if="card.deck === 'SmallDeal' || card.deck === 'BigDeal'"
          type="button"
          class="gold"
          :disabled="busy"
          @click="emit('buy', isStock ? shares : null)"
        >
          Buy
        </button>
        <button
          type="button"
          class="ghost"
          :disabled="busy"
          @click="emit('pass')"
        >
          Pass
        </button>
      </div>
      <p
        v-else
        class="waiting"
      >
        The clone is looking at this card.
      </p>
    </article>
  </div>
</template>

<style scoped>
.shade {
  position: fixed;
  inset: 0;
  background: rgba(6, 8, 7, 0.55);
  display: grid;
  place-items: center;
  padding: 1rem;
  z-index: 5;
}

.card {
  width: min(24rem, 100%);
  padding: 1rem 1.1rem 1.15rem;
}

img {
  width: 4.5rem;
  height: auto;
}

.kind {
  margin: 0.4rem 0 0;
  color: var(--gold);
  letter-spacing: 0.14em;
  text-transform: uppercase;
  font-size: 0.68rem;
}

h2 {
  margin: 0.2rem 0 0.35rem;
  font-family: var(--serif);
  font-weight: 400;
  font-size: 1.8rem;
}

.summary,
.waiting {
  margin: 0;
  color: var(--muted);
}

ul {
  padding: 0;
  margin: 0.8rem 0;
  list-style: none;
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

li {
  border: 1px solid rgba(239, 231, 214, 0.16);
  border-radius: 999px;
  padding: 0.25rem 0.6rem;
  font-size: 0.82rem;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
  align-items: end;
}

label {
  display: grid;
  gap: 0.25rem;
  font-size: 0.75rem;
  color: var(--muted);
}

input {
  width: 5rem;
  border-radius: 10px;
  border: 1px solid rgba(239, 231, 214, 0.2);
  background: transparent;
  color: var(--paper);
  padding: 0.4rem 0.5rem;
}

button {
  border: 0;
  border-radius: 999px;
  padding: 0.55rem 0.95rem;
  cursor: pointer;
}

.gold {
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
