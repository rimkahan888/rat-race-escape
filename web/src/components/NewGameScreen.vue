<script setup lang="ts">
import { computed, ref } from 'vue'
import { professions, tokenColours, tokenSrc } from '../catalog'
import type { SeatRequest } from '../types'

const emit = defineEmits<{
  start: [payload: { players: SeatRequest[]; clone: boolean; seed: number | null }]
}>()

const mode = ref<'clone' | 'people'>('clone')
const name = ref('Ava')
const professionId = ref('night-baker')
const token = ref('Amber')
const seedText = ref('')
const people = ref<SeatRequest[]>([
  { name: 'Ava', professionId: 'night-baker', tokenColour: 'Amber' },
  { name: 'Bo', professionId: 'municipal-archivist', tokenColour: 'Cobalt' },
])

const seed = computed(() => {
  const trimmed = seedText.value.trim()
  if (!trimmed) return null
  const value = Number(trimmed)
  return Number.isInteger(value) ? value : null
})

function addPerson() {
  if (people.value.length >= 4) return
  const used = new Set(people.value.map((seat) => seat.tokenColour))
  const colour = tokenColours.find((item) => !used.has(item)) ?? 'Fern'
  people.value.push({
    name: '',
    professionId: professions[people.value.length % professions.length].id,
    tokenColour: colour,
  })
}

function removePerson(index: number) {
  if (people.value.length <= 2) return
  people.value.splice(index, 1)
}

function submit() {
  if (mode.value === 'clone') {
    emit('start', {
      clone: true,
      seed: seed.value,
      players: [{ name: name.value, professionId: professionId.value, tokenColour: token.value }],
    })
    return
  }
  emit('start', { clone: false, seed: seed.value, players: people.value.map((seat) => ({ ...seat })) })
}
</script>

<template>
  <form
    class="setup glass"
    @submit.prevent="submit"
  >
    <p class="eyebrow">
      New table
    </p>
    <div
      class="modes"
      role="tablist"
      aria-label="Who is playing"
    >
      <button
        type="button"
        :class="{ on: mode === 'clone' }"
        :aria-selected="mode === 'clone'"
        @click="mode = 'clone'"
      >
        Me vs my clone
      </button>
      <button
        type="button"
        :class="{ on: mode === 'people' }"
        :aria-selected="mode === 'people'"
        @click="mode = 'people'"
      >
        2 to 4 people
      </button>
    </div>

    <template v-if="mode === 'clone'">
      <label>
        Name
        <input
          v-model="name"
          required
          minlength="2"
          maxlength="40"
          autocomplete="nickname"
        >
      </label>
      <label>
        Profession
        <select v-model="professionId">
          <option
            v-for="job in professions"
            :key="job.id"
            :value="job.id"
          >
            {{ job.title }}
          </option>
        </select>
      </label>
      <fieldset>
        <legend>Token</legend>
        <div class="swatches">
          <label
            v-for="colour in tokenColours"
            :key="colour"
            class="swatch"
            :class="colour.toLowerCase()"
          >
            <input
              v-model="token"
              type="radio"
              name="token"
              :value="colour"
            >
            <img
              :src="tokenSrc(colour)"
              alt=""
            >
          </label>
        </div>
      </fieldset>
    </template>

    <template v-else>
      <div
        v-for="(seat, index) in people"
        :key="index"
        class="person"
      >
        <label>
          Name
          <input
            v-model="seat.name"
            required
            minlength="2"
            maxlength="40"
          >
        </label>
        <label>
          Profession
          <select v-model="seat.professionId">
            <option
              v-for="job in professions"
              :key="job.id"
              :value="job.id"
            >
              {{ job.title }}
            </option>
          </select>
        </label>
        <label>
          Token
          <select v-model="seat.tokenColour">
            <option
              v-for="colour in tokenColours"
              :key="colour"
              :value="colour"
            >
              {{ colour }}
            </option>
          </select>
        </label>
        <button
          v-if="people.length > 2"
          type="button"
          class="ghost"
          @click="removePerson(index)"
        >
          Remove
        </button>
      </div>
      <button
        v-if="people.length < 4"
        type="button"
        class="ghost"
        @click="addPerson"
      >
        Add a player
      </button>
    </template>

    <label>
      Seed, optional
      <input
        v-model="seedText"
        inputmode="numeric"
        placeholder="Same number, same game"
      >
    </label>
    <button
      type="submit"
      class="gold"
    >
      Sit down
    </button>
  </form>
</template>

<style scoped>
.setup {
  width: min(28rem, 100%);
  display: grid;
  gap: 0.85rem;
  padding: 1.1rem 1.15rem 1.25rem;
}

.eyebrow {
  margin: 0;
  color: var(--gold);
  letter-spacing: 0.16em;
  text-transform: uppercase;
  font-size: 0.72rem;
}

.modes {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.4rem;
}

.modes button,
.ghost,
.gold,
select,
input {
  border-radius: 12px;
  border: 1px solid rgba(239, 231, 214, 0.16);
  background: rgba(8, 10, 8, 0.45);
  color: var(--paper);
  padding: 0.55rem 0.7rem;
}

.modes button.on,
.gold {
  background: linear-gradient(180deg, #f0d48a, #c4923a);
  color: #24180a;
  border-color: transparent;
  font-weight: 600;
}

label,
fieldset {
  display: grid;
  gap: 0.35rem;
  margin: 0;
  border: 0;
  padding: 0;
  font-size: 0.82rem;
  color: var(--muted);
}

.person {
  display: grid;
  gap: 0.45rem;
  padding-bottom: 0.6rem;
  border-bottom: 1px solid rgba(239, 231, 214, 0.08);
}

.swatches {
  display: flex;
  gap: 0.45rem;
  flex-wrap: wrap;
}

.swatch {
  position: relative;
  width: 3.1rem;
}

.swatch img {
  width: 100%;
  height: auto;
  display: block;
  filter: saturate(0.75);
}

.swatch:has(input:checked) img {
  filter: none;
  outline: 2px solid var(--gold);
  border-radius: 999px;
}

.swatch input {
  position: absolute;
  opacity: 0;
}

.ghost {
  justify-self: start;
}

.gold {
  cursor: pointer;
}
</style>
