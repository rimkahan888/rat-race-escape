<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { fastTrackSpots, ratRaceSpots } from '../boardLayout'
import { tokenSrc } from '../catalog'
import type { PlayerState } from '../types'

const props = defineProps<{
  players: PlayerState[]
  currentIndex: number
}>()

const portrait = ref(false)
let media: ReturnType<typeof window.matchMedia> | null = null

function readPortrait() {
  portrait.value = window.matchMedia('(orientation: portrait), (max-aspect-ratio: 3/4)').matches
}

onMounted(() => {
  readPortrait()
  media = window.matchMedia('(orientation: portrait), (max-aspect-ratio: 3/4)')
  media.addEventListener('change', readPortrait)
})

onUnmounted(() => {
  media?.removeEventListener('change', readPortrait)
})

const rat = computed(() => ratRaceSpots(portrait.value))
const fast = computed(() => fastTrackSpots(portrait.value))

function spot(player: PlayerState, seat: number) {
  const base = player.track === 'FastTrack' ? fast.value[player.boardIndex] : rat.value[player.boardIndex]
  const nudge = seat - (props.players.length - 1) / 2
  return {
    left: `${base.x}%`,
    top: `${base.y}%`,
    transform: `translate(-50%, -82%) translateX(${nudge * 28}px)`,
  }
}
</script>

<template>
  <div
    class="island"
    aria-label="Game table"
  >
    <picture>
      <source
        type="image/avif"
        media="(orientation: portrait), (max-aspect-ratio: 3/4)"
        srcset="/art/bg-portrait.avif"
      >
      <source
        type="image/avif"
        srcset="/art/bg-landscape.avif"
      >
      <source
        type="image/webp"
        media="(orientation: portrait), (max-aspect-ratio: 3/4)"
        srcset="/art/bg-portrait-470.webp 470w, /art/bg-portrait.webp 941w"
        sizes="100vw"
      >
      <source
        type="image/webp"
        srcset="/art/bg-landscape-836.webp 836w, /art/bg-landscape.webp 1672w"
        sizes="100vw"
      >
      <img
        class="plate"
        src="/art/bg-landscape.webp"
        alt=""
      >
    </picture>
    <img
      v-for="(player, index) in players"
      :key="player.name"
      class="piece"
      :class="[player.tokenColour.toLowerCase(), { current: index === currentIndex }]"
      :src="tokenSrc(player.tokenColour)"
      :style="spot(player, index)"
      :alt="player.name"
    >
  </div>
</template>

<style scoped>
.island {
  position: relative;
  width: min(100%, 920px);
  margin: 0 auto;
}

.plate {
  width: 100%;
  height: auto;
  display: block;
  border-radius: 28px;
  filter: drop-shadow(0 30px 40px rgba(0, 0, 0, 0.45));
}

.piece {
  position: absolute;
  width: 9%;
  height: auto;
  pointer-events: none;
  filter: drop-shadow(0 8px 6px rgba(0, 0, 0, 0.45));
}

.piece.current {
  z-index: 2;
  outline: 2px solid var(--gold);
  border-radius: 999px;
}

.piece.violet {
  filter: hue-rotate(28deg) drop-shadow(0 8px 6px rgba(0, 0, 0, 0.45));
}

.piece.slate {
  filter: grayscale(0.8) drop-shadow(0 8px 6px rgba(0, 0, 0, 0.45));
}
</style>
