// Category colour system + small value helpers (ADR-025). Kept out of ui.tsx so
// that file can export only components (fast-refresh requirement).

// One harmonious OKLCH set, hue per category.
export const cat = (hue: number, l = 0.7, c = 0.13) => `oklch(${l} ${c} ${hue})`
export const catSoft = (hue: number) => `oklch(0.95 0.045 ${hue})`
export const catTint = (hue: number) => `oklch(0.97 0.03 ${hue})`
export const catInk = (hue: number) => `oklch(0.46 0.11 ${hue})`

// Backend gender slugs → the tiny, low-opacity glyph shown beside a name.
export const GENDER_GLYPH: Record<string, string> = {
  woman: '♀',
  man: '♂',
  non_binary: '⚧',
  self_describe: '·',
}

export function genderGlyph(gender: string | null | undefined): string {
  return (gender && GENDER_GLYPH[gender]) || '·'
}

// A stable hue from a profile id, so the monogram fallback is consistent per person.
export function hueFromId(id: string): number {
  let h = 0
  for (let i = 0; i < id.length; i++) {
    h = (h * 31 + id.charCodeAt(i)) % 360
  }
  return h
}
