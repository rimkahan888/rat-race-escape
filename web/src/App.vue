<script setup lang="ts">
import { computed, ref } from 'vue'
import * as api from './api'
import { TableError } from './api'
import DealModal from './components/DealModal.vue'
import DiceTray from './components/DiceTray.vue'
import ExitMark from './components/ExitMark.vue'
import IslandTable from './components/IslandTable.vue'
import NewGameScreen from './components/NewGameScreen.vue'
import PlayerCard from './components/PlayerCard.vue'
import { spaceIcon } from './catalog'
import type { GameState, SeatRequest } from './types'

const game = ref<GameState | null>(null)
const busy = ref(false)
const notice = ref('')
let follow = 0

const current = computed(() => game.value?.players[game.value.currentTurnIndex] ?? null)
const humanTurn = computed(() => current.value != null && !current.value.isClone && game.value?.phase === 'RatRace')
const you = computed(() => game.value?.players.find((player) => !player.isClone) ?? game.value?.players[0] ?? null)
const clone = computed(() => game.value?.players.find((player) => player.isClone) ?? null)
const others = computed(() => game.value?.players.filter((player) => player !== you.value && player !== clone.value) ?? [])

const choice = computed(() => {
  if (!humanTurn.value || game.value?.turnPhase !== 'AwaitingResolve' || !current.value) return null
  if (current.value.track === 'FastTrack' && current.value.spaceKind === 'Dream') return 'dream'
  if (current.value.track === 'RatRace' && current.value.spaceKind === 'Charity') return 'charity'
  if (current.value.track === 'RatRace' && current.value.spaceKind === 'Deal') return 'deal'
  return 'continue'
})

async function run(action: () => Promise<GameState>) {
  busy.value = true
  notice.value = ''
  try {
    const next = await action()
    game.value = next
    void followClone(next)
  } catch (error) {
    notice.value = error instanceof TableError ? error.message : 'The table could not do that.'
    if (/doodad|downsized|dry dock/i.test(notice.value)) {
      try {
        const settled = await api.settle(game.value!.id)
        game.value = settled
        notice.value = ''
        void followClone(settled)
      } catch (settleError) {
        notice.value = settleError instanceof TableError ? settleError.message : notice.value
      }
    }
  } finally {
    busy.value = false
  }
}

async function followClone(snapshot: GameState) {
  const token = ++follow
  let table = snapshot
  while (token === follow && table.phase === 'RatRace') {
    const seat = table.players[table.currentTurnIndex]
    if (!seat?.isClone) return
    await new Promise((resolve) => {
      window.setTimeout(resolve, 900)
    })
    if (token !== follow) return
    try {
      table = await api.cloneStep(table.id)
      if (token !== follow) return
      game.value = table
    } catch (error) {
      notice.value = error instanceof TableError ? error.message : 'The clone paused.'
      return
    }
  }
}

function start(payload: { players: SeatRequest[]; clone: boolean; seed: number | null }) {
  void run(() => api.startGame(payload.players, payload.clone, payload.seed))
}

function onRoll(count: number) {
  if (!game.value) return
  void run(() => api.roll(game.value!.id, count))
}

function onResolve(next: string) {
  if (!game.value) return
  void run(() => api.resolve(game.value!.id, next))
}

function onBuy(shares: number | null) {
  if (!game.value) return
  void run(() => api.buy(game.value!.id, shares))
}

function onPass() {
  if (!game.value) return
  void run(() => api.passCard(game.value!.id))
}

function onEnd() {
  if (!game.value) return
  void run(() => api.endTurn(game.value!.id))
}

function onLeave() {
  if (!game.value) return
  void run(() => api.leave(game.value!.id))
}

function onBorrow() {
  if (!game.value) return
  void run(() => api.borrow(game.value!.id, 1))
}

function onRepay() {
  if (!game.value) return
  void run(() => api.repay(game.value!.id, 1))
}

function onFastDeal(id: string) {
  if (!game.value) return
  void run(() => api.buyFastTrackDeal(game.value!.id, id))
}

const headline = computed(() => {
  if (!game.value) return 'No game in progress'
  if (game.value.phase === 'Won' && game.value.winnerIndex != null) {
    return `${game.value.players[game.value.winnerIndex]?.name ?? 'Someone'} bought the dream`
  }
  if (game.value.phase === 'Over') return 'The table is clear'
  if (current.value?.isClone) return 'Clone is moving'
  return `${current.value?.name ?? 'You'} to play`
})
</script>

<template>
  <div
    class="shell"
    aria-label="Game table"
  >
    <header class="top">
      <p class="mark">
        <ExitMark />
        <span>Rat Race Escape</span>
      </p>
      <p class="session">
        {{ headline }}
      </p>
    </header>

    <main v-if="!game">
      <NewGameScreen @start="start" />
      <p
        v-if="notice"
        class="notice"
        role="status"
      >
        {{ notice }}
      </p>
    </main>

    <main
      v-else
      class="table"
    >
      <IslandTable
        :players="game.players"
        :current-index="game.currentTurnIndex"
      />
      <div class="panels">
        <PlayerCard
          v-if="you"
          :player="you"
          :active="current === you"
          :yours="humanTurn && current === you"
          :busy="busy"
          @borrow="onBorrow"
          @repay="onRepay"
          @leave="onLeave"
        />
        <PlayerCard
          v-if="clone"
          :player="clone"
          :active="current === clone"
          :yours="false"
          :busy="busy"
        />
        <PlayerCard
          v-for="player in others"
          :key="player.name"
          :player="player"
          :active="current === player"
          :yours="humanTurn && current === player"
          :busy="busy"
          @borrow="onBorrow"
          @repay="onRepay"
          @leave="onLeave"
        />
        <DiceTray
          :faces="game.lastRoll?.faces ?? []"
          :can-roll="humanTurn && game.turnPhase === 'AwaitingRoll'"
          :two-dice="(current?.extraDieTurnsRemaining ?? 0) > 0"
          :can-end="humanTurn && game.turnPhase === 'AwaitingEnd'"
          :busy="busy"
          @roll="onRoll"
          @end="onEnd"
        />
        <div
          v-if="choice"
          class="choices glass"
        >
          <img
            :src="spaceIcon(current?.spaceKind ?? '', current?.track ?? 'RatRace')"
            alt=""
          >
          <p>{{ current?.spaceTitle }}</p>
          <button
            v-if="choice === 'continue'"
            type="button"
            class="gold"
            :disabled="busy"
            @click="onResolve('None')"
          >
            Continue
          </button>
          <template v-else-if="choice === 'charity'">
            <button
              type="button"
              class="gold"
              :disabled="busy"
              @click="onResolve('Donate')"
            >
              Give
            </button>
            <button
              type="button"
              :disabled="busy"
              @click="onResolve('Decline')"
            >
              Decline
            </button>
          </template>
          <template v-else-if="choice === 'deal'">
            <button
              type="button"
              class="gold"
              :disabled="busy"
              @click="onResolve('SmallDeal')"
            >
              Small
            </button>
            <button
              type="button"
              :disabled="busy"
              @click="onResolve('BigDeal')"
            >
              Big
            </button>
          </template>
          <template v-else>
            <button
              type="button"
              class="gold"
              :disabled="busy"
              @click="onResolve('ClaimDream')"
            >
              Claim
            </button>
            <button
              type="button"
              :disabled="busy"
              @click="onResolve('Decline')"
            >
              Walk on
            </button>
          </template>
        </div>
        <div
          v-if="humanTurn && current?.track === 'FastTrack' && game.turnPhase !== 'AwaitingResolve'"
          class="deals glass"
        >
          <button
            v-for="deal in game.fastTrackDeals"
            :key="deal.id"
            type="button"
            :disabled="busy"
            @click="onFastDeal(deal.id)"
          >
            <img
              src="/art/yacht.webp"
              alt=""
            >
            {{ deal.title }}
          </button>
        </div>
      </div>
      <p
        v-if="notice"
        class="notice"
        role="status"
      >
        {{ notice }}
      </p>
      <DealModal
        v-if="game.drawn"
        :card="game.drawn"
        :busy="busy"
        :yours="humanTurn"
        @buy="onBuy"
        @pass="onPass"
      />
    </main>
  </div>
</template>

<style scoped>
.shell {
  min-height: 100vh;
  display: grid;
  grid-template-rows: auto 1fr;
  padding: 1rem clamp(0.8rem, 2vw, 1.6rem) 1.4rem;
}

.top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0.8rem;
}

.mark {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  margin: 0;
}

.session {
  margin: 0;
  color: var(--muted);
}

main {
  display: grid;
  align-content: start;
  justify-items: center;
  gap: 0.8rem;
}

.table {
  grid-template-columns: minmax(0, 1fr) minmax(16rem, 22rem);
  align-items: start;
}

.panels {
  display: grid;
  gap: 0.7rem;
}

.choices,
.deals {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
  align-items: center;
  padding: 0.7rem;
}

.choices img,
.deals img {
  width: 1.8rem;
  height: auto;
}

.choices p {
  margin: 0;
  flex: 1 1 8rem;
}

button {
  border-radius: 999px;
  border: 1px solid rgba(239, 231, 214, 0.18);
  background: transparent;
  color: var(--paper);
  padding: 0.45rem 0.75rem;
  cursor: pointer;
}

.gold {
  background: linear-gradient(180deg, #f0d48a, #c4923a);
  color: #24180a;
  border-color: transparent;
  font-weight: 650;
}

.notice {
  margin: 0;
  color: #f0d48a;
}

@media (max-width: 900px), (orientation: portrait) and (max-aspect-ratio: 3/4) {
  .table {
    grid-template-columns: 1fr;
  }
}
</style>
