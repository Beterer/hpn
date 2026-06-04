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
