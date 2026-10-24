<template>
  <section
    class="table"
    aria-label="Game table"
  >
    <p class="kicker">
      The table
    </p>
    <h1>The month still owns the calendar.</h1>
    <p class="lede">
      A profession, a paycheck, and the bills that follow it. The way off the track is not drawn yet.
    </p>
  </section>
</template>

<style scoped>
.table {
  align-self: end;
  max-width: 40rem;
  margin-top: auto;
  margin-bottom: 7vh;
  padding-top: 1.5rem;
  border-top: 1px solid var(--line);
}

.kicker {
  margin: 0 0 0.85rem;
  color: var(--exit);
  font-size: 0.72rem;
  font-weight: 500;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

h1 {
  margin: 0;
  max-width: 12ch;
  font-family: var(--serif);
  font-size: clamp(2.7rem, 7vw, 4.75rem);
  font-weight: 400;
  letter-spacing: -0.03em;
  line-height: 0.92;
}

.lede {
  max-width: 34rem;
  margin: 1.15rem 0 0;
  color: var(--muted);
  font-size: 1.05rem;
}
</style>
