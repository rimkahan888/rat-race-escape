export const professions = [
  { id: 'night-baker', title: 'Night-Shift Baker' },
  { id: 'municipal-archivist', title: 'Municipal Archivist' },
  { id: 'harbor-ferry-mate', title: 'Harbor Ferry Mate' },
  { id: 'clinic-lab-tech', title: 'Clinic Lab Technologist' },
  { id: 'grid-electrician', title: 'Grid Maintenance Electrician' },
  { id: 'trail-designer', title: 'Regional Trail Designer' },
  { id: 'tunnel-inspector', title: 'Alpine Tunnel Inspector' },
  { id: 'reef-surveyor', title: 'Coral Reef Surveyor' },
] as const

export const tokenColours = ['Amber', 'Cobalt', 'Fern', 'Coral', 'Violet', 'Slate'] as const

export type TokenColourName = (typeof tokenColours)[number]

export function tokenSrc(colour: string): string {
  if (colour === 'Amber') return '/art/token-amber.webp'
  if (colour === 'Cobalt' || colour === 'Slate') return '/art/token-cobalt.webp'
  if (colour === 'Coral' || colour === 'Violet') return '/art/token-coral.webp'
  return '/art/token-fern.webp'
}

export function spaceIcon(kind: string, track: string): string {
  if (track === 'FastTrack') {
    if (kind === 'Dream') return '/art/mansion.webp'
    if (kind === 'Setback') return '/art/yacht.webp'
    if (kind === 'Windfall') return '/art/coins.webp'
    return '/art/tile-fast.webp'
  }
  if (kind === 'Doodad') return '/art/bag.webp'
  if (kind === 'Deal') return '/art/briefcase.webp'
  if (kind === 'Baby') return '/art/apartment.webp'
  if (kind === 'Charity') return '/art/palm.webp'
  if (kind === 'Downsized') return '/art/office.webp'
  if (kind === 'Payday' || kind === 'Market') return '/art/coins.webp'
  return '/art/tile-rat.webp'
}
