<script setup lang="ts">
import { computed } from 'vue'
import FlowRing from './FlowRing.vue'
import { spaceIcon, tokenSrc } from '../catalog'
import { formatFlow, formatMoney } from '../money'
import type { PlayerState } from '../types'

const props = defineProps<{
  player: PlayerState
  active: boolean
  yours: boolean
  busy: boolean
}>()

const emit = defineEmits<{
  borrow: []
  repay: []
  leave: []
}>()

const escapeRatio = computed(() => {
  if (props.player.totalExpensesCents <= 0) return 0
  return Math.min(100, Math.round((Math.max(0, props.player.passiveIncomeCents) / props.player.totalExpensesCents) * 100))
})

const stocks = computed(() => {
  const grouped = new Map<string, number>()
  for (const lot of props.player.stocks) {
    grouped.set(lot.symbol, (grouped.get(lot.symbol) ?? 0) + lot.shares)
  }
  return [...grouped.entries()]
})

const homes = computed(() => {
  const grouped = new Map<string, { count: number; flow: number }>()
  for (const lot of props.player.realEstate) {
    const current = grouped.get(lot.name) ?? { count: 0, flow: 0 }
    current.count += 1
    current.flow += lot.monthlyCashFlowCents
    grouped.set(lot.name, current)
  }
  return [...grouped.entries()]
})

const shops = computed(() => {
  const grouped = new Map<string, { count: number; flow: number }>()
  for (const lot of props.player.businesses) {
    const current = grouped.get(lot.name) ?? { count: 0, flow: 0 }
    current.count += 1
    current.flow += lot.monthlyCashFlowCents
    grouped.set(lot.name, current)
  }
  return [...grouped.entries()]
})
</script>

<template>
  <article
    class="card glass"
    :class="{ active, clone: player.isClone, broke: player.isBankrupt }"
  >
    <header>
      <img
        class="token"
        :class="player.tokenColour.toLowerCase()"
        :src="tokenSrc(player.tokenColour)"
        alt=""
      >
      <div>
        <p class="who">
          {{ player.isClone ? 'Clone' : 'Player' }}
        </p>
        <h2>{{ player.name }}</h2>
        <p class="job">
          {{ player.professionTitle }}
        </p>
      </div>
      <FlowRing
        :passive-cents="Math.max(0, player.passiveIncomeCents)"
        :expense-cents="Math.max(0, player.totalExpensesCents)"
        :cash-flow-cents="Math.max(0, player.monthlyCashFlowCents)"
      />
    </header>
    <div
      class="bar"
      role="meter"
      :aria-valuenow="escapeRatio"
      aria-valuemin="0"
      aria-valuemax="100"
      aria-label="Escape progress"
    >
      <span :style="{ width: `${escapeRatio}%` }" />
    </div>
    <dl>
      <div>
        <dt>Cash</dt>
        <dd>{{ formatMoney(player.cashCents) }}</dd>
      </div>
      <div>
        <dt>Cash flow</dt>
        <dd>{{ formatMoney(player.monthlyCashFlowCents) }}</dd>
      </div>
      <div>
        <dt>Passive</dt>
        <dd>{{ formatFlow(player.passiveIncomeCents) }}</dd>
      </div>
      <div>
        <dt>Bills</dt>
        <dd>{{ formatMoney(player.totalExpensesCents) }}</dd>
      </div>
    </dl>
    <p class="standing">
      <img
        :src="spaceIcon(player.spaceKind, player.track)"
        alt=""
      >
      {{ player.spaceTitle }}
    </p>
    <ul v-if="stocks.length || homes.length || shops.length">
      <li
        v-for="[symbol, shares] in stocks"
        :key="symbol"
      >
        <img
          src="/art/coins.webp"
          alt=""
        >
        {{ symbol }} · {{ shares }}
      </li>
      <li
        v-for="[name, lot] in homes"
        :key="name"
      >
        <img
          src="/art/apartment.webp"
          alt=""
        >
        {{ name }}<template v-if="lot.count > 1">
          · {{ lot.count }}
        </template>
      </li>
      <li
        v-for="[name, lot] in shops"
        :key="name"
      >
        <img
          src="/art/office.webp"
          alt=""
        >
        {{ name }}<template v-if="lot.count > 1">
          · {{ lot.count }}
        </template>
      </li>
    </ul>
    <div
      v-if="yours && !player.isBankrupt"
      class="row"
    >
      <button
        type="button"
        :disabled="busy"
        @click="emit('borrow')"
      >
        Borrow
      </button>
      <button
        type="button"
        :disabled="busy"
        @click="emit('repay')"
      >
        Repay
      </button>
      <button
        v-if="player.passiveIncomeExceedsExpenses && player.track === 'RatRace'"
        type="button"
        class="leave"
        :disabled="busy"
        @click="emit('leave')"
      >
        Leave
      </button>
    </div>
  </article>
</template>

<style scoped>
.card {
  padding: 0.85rem;
  display: grid;
  gap: 0.55rem;
}

.card.active {
  outline: 1px solid rgba(240, 212, 138, 0.7);
}

header {
  display: grid;
  grid-template-columns: auto 1fr auto;
  gap: 0.55rem;
  align-items: center;
}

.token {
  width: 3.1rem;
  height: auto;
}

.token.violet {
  filter: hue-rotate(28deg);
}

.token.slate {
  filter: grayscale(0.85);
}

.who,
.job {
  margin: 0;
  color: var(--muted);
  font-size: 0.75rem;
}

h2 {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 560;
}

.bar {
  height: 0.35rem;
  border-radius: 999px;
  background: rgba(239, 231, 214, 0.12);
  overflow: hidden;
}

.bar span {
  display: block;
  height: 100%;
  background: linear-gradient(90deg, var(--green), var(--gold));
}

dl {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.35rem 0.8rem;
  margin: 0;
}

dt {
  color: var(--muted);
  font-size: 0.68rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

dd {
  margin: 0;
}

.standing,
li {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.standing {
  margin: 0;
  font-size: 0.86rem;
}

.standing img,
li img {
  width: 1.4rem;
  height: auto;
}

ul {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 0.25rem;
  font-size: 0.82rem;
}

.row {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
}

button {
  border-radius: 999px;
  border: 1px solid rgba(239, 231, 214, 0.18);
  background: transparent;
  color: var(--paper);
  padding: 0.35rem 0.7rem;
  cursor: pointer;
}

.leave {
  background: var(--green);
  color: #10210f;
  border-color: transparent;
}
</style>
