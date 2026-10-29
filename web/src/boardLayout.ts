export interface Spot {
  x: number
  y: number
}

function oval(count: number, cx: number, cy: number, rx: number, ry: number): Spot[] {
  const spots: Spot[] = []
  for (let index = 0; index < count; index += 1) {
    const angle = -Math.PI / 2 + (index / count) * Math.PI * 2
    spots.push({
      x: cx + Math.cos(angle) * rx,
      y: cy + Math.sin(angle) * ry,
    })
  }
  return spots
}

export function ratRaceSpots(portrait: boolean): Spot[] {
  return portrait
    ? oval(24, 50, 47.6, 27, 9.2)
    : oval(24, 49.6, 41.2, 17.6, 14.6)
}

export function fastTrackSpots(portrait: boolean): Spot[] {
  return portrait
    ? oval(12, 50, 48.2, 40, 13.4)
    : oval(12, 49.8, 42.4, 27.2, 22.6)
}
