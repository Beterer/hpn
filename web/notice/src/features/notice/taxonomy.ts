import type { AppreciationCategory } from '../../lib/api/appreciation'

// The 6 categories' fixed OKLCH hues + canonical order (ADR-025). Used to colour
// and order the radar axes, which read category slug + share but not hue.
export const CATEGORY_HUE: Record<string, number> = {
  physical: 38,
  energy: 78,
  style: 350,
  humor: 142,
  mind: 264,
  authentic: 200,
}
export const CATEGORY_ORDER = ['physical', 'energy', 'style', 'humor', 'mind', 'authentic']

/** A trait flattened out of its category, carrying the category's hue for colour. */
export interface FlatTrait {
  id: string
  label: string
  hue: number
  categoryId: string
  categorySlug: string
}

// The flattened picker cloud (ADR-025): all active traits at once, in category
// order so colours cluster like a soft rainbow. Category survives as the hue.
export function flattenTraits(categories: AppreciationCategory[] | undefined): FlatTrait[] {
  if (!categories) {
    return []
  }
  return [...categories]
    .sort((a, b) => Number(a.sortOrder) - Number(b.sortOrder))
    .flatMap((c) =>
      (c.traits ?? []).map((t) => ({
        id: t.id,
        label: t.label,
        hue: Number(c.hue),
        categoryId: c.id,
        categorySlug: c.slug,
      })),
    )
}

// Short, human blurbs shown under each lane label in the guided picker.
// Keyed by category slug so they survive even if the API has no description field.
export const CATEGORY_BLURB: Record<string, string> = {
  physical: 'the way they look',
  energy: 'how they come across',
  style: 'how they put themselves together',
  humor: 'they made the moment lighter',
  mind: "what's going on upstairs",
  authentic: 'they feel real',
}

/** A category with its traits kept grouped (for the guided picker). */
export interface TraitGroup {
  categoryId: string
  slug: string
  label: string
  hue: number
  blurb: string
  traits: FlatTrait[]
}

// Same source + ordering as flattenTraits, but grouped by category instead of
// flattened — so the picker can show lanes first, then the words inside one.
export function groupTraits(categories: AppreciationCategory[] | undefined): TraitGroup[] {
  if (!categories) {
    return []
  }
  return [...categories]
    .sort((a, b) => Number(a.sortOrder) - Number(b.sortOrder))
    .map((c) => ({
      categoryId: c.id,
      slug: c.slug,
      label: c.label,
      hue: Number(c.hue),
      blurb: CATEGORY_BLURB[c.slug] ?? '',
      traits: (c.traits ?? []).map((t) => ({
        id: t.id,
        label: t.label,
        hue: Number(c.hue),
        categoryId: c.id,
        categorySlug: c.slug,
      })),
    }))
    .filter((g) => g.traits.length > 0)
}
