// data.jsx — design tokens, reaction taxonomy, and mock content for Notice.
// Exposed on window for the other babel scripts.

// ── Reaction taxonomy: category → specific traits ───────────────────────────
// Each category shares lightness/chroma, varies hue (harmonious accent set).
const CATEGORIES = [
  {
    id: 'physical',
    label: 'Physical',
    blurb: 'the way they look',
    hue: 38,
    traits: ['Warm smile', 'Kind eyes', 'Great hair', 'Natural glow'],
  },
  {
    id: 'energy',
    label: 'Energy',
    blurb: 'how they come across',
    hue: 78,
    traits: ['Good vibe', 'Confident', 'Calm presence', 'Magnetic'],
  },
  {
    id: 'style',
    label: 'Style',
    blurb: 'how they put themselves together',
    hue: 350,
    traits: ['Great fit', 'Effortless', 'Signature look'],
  },
  {
    id: 'humor',
    label: 'Humor',
    blurb: 'they made the moment lighter',
    hue: 142,
    traits: ['Made me grin', 'Quick wit', 'Wonderfully odd'],
  },
  {
    id: 'mind',
    label: 'Mind',
    blurb: "what's going on upstairs",
    hue: 264,
    traits: ['Curious', 'Thoughtful', 'Sharp'],
  },
  {
    id: 'authentic',
    label: 'Authentic',
    blurb: 'they feel real',
    hue: 200,
    traits: ['Genuine', 'Grounded', 'True to themselves'],
  },
];

// Accent helpers — one harmonious system, hue per category.
const cat = (hue, l = 0.7, c = 0.13) => `oklch(${l} ${c} ${hue})`;
const catSoft = (hue) => `oklch(0.95 0.045 ${hue})`;
const catTint = (hue) => `oklch(0.97 0.03 ${hue})`;
const catInk = (hue) => `oklch(0.46 0.11 ${hue})`;

const CATEGORY_BY_ID = Object.fromEntries(CATEGORIES.map((c) => [c.id, c]));

// Flattened trait cloud — category survives as color (hue), not as a click.
const ALL_TRAITS = CATEGORIES.flatMap((c) =>
  c.traits.map((label) => ({ label, category: c.id, hue: c.hue })),
);

// ── Feed profiles (other people you can appreciate) ─────────────────────────
// gender glyph uses unicode marks shown tiny + transparent (♀ ♂ ⚧).
const GENDER_GLYPH = { woman: '♀', man: '♂', nonbinary: '⚧', undisclosed: '·' };

const PROFILES = [
  {
    id: 'p-mara', name: 'Mara', gender: 'woman', country: 'RO', distance: 'Nearby',
    tone: 28, mono: 'M',
    interests: ['Pottery', 'Trail running', 'Vinyl'],
    bio: 'Makes a strong espresso and a stronger argument.',
  },
  {
    id: 'p-theo', name: 'Theo', gender: 'man', country: 'PT', distance: 'Under 50 km',
    tone: 200, mono: 'T',
    interests: ['Surfing', 'Film photography', 'Cooking'],
    bio: 'Probably outside right now.',
  },
  {
    id: 'p-juno', name: 'Juno', gender: 'nonbinary', country: 'NL', distance: '50–200 km',
    tone: 264, mono: 'J',
    interests: ['Synths', 'Cycling', 'Sci-fi'],
    bio: 'Building small machines that make sound.',
  },
  {
    id: 'p-sofia', name: 'Sofia', gender: 'woman', country: 'IT', distance: 'Nearby',
    tone: 350, mono: 'S',
    interests: ['Climbing', 'Ceramics', 'Markets'],
    bio: 'Weekend mountains, weekday sketchbook.',
  },
  {
    id: 'p-daniel', name: 'Daniel', gender: 'man', country: 'DE', distance: 'Different country',
    tone: 142, mono: 'D',
    interests: ['Chess', 'Jazz', 'Long walks'],
    bio: 'Slow mornings, longer playlists.',
  },
  {
    id: 'p-aria', name: 'Aria', gender: 'woman', country: 'ES', distance: 'Under 50 km',
    tone: 78, mono: 'A',
    interests: ['Painting', 'Tea', 'Bouldering'],
    bio: 'Color first, words later.',
  },
];

// ── "You": received appreciation + fingerprint (private readings) ───────────
const RECEIVED = {
  headline: 'Quietly, people keep noticing you.',
  summary:
    'These are the words others chose, kept private and shown only to you. No counts are ever made public.',
  total: 34,
  // ordered by how often, each maps to a category for color
  traits: [
    { label: 'Good vibe', category: 'energy', count: 7, phrasing: 'Your energy reads as easy and open.' },
    { label: 'Warm smile', category: 'physical', count: 5, phrasing: 'People often notice your warm smile.' },
    { label: 'Calm presence', category: 'energy', count: 5, phrasing: 'Being around you tends to feel calm.' },
    { label: 'Genuine', category: 'authentic', count: 4, phrasing: 'People experience you as real and unguarded.' },
    { label: 'Thoughtful', category: 'mind', count: 3, phrasing: 'People notice the care behind what you say.' },
    { label: 'Effortless', category: 'style', count: 2, phrasing: 'Your way of putting things together looks easy.' },
  ],
  recent: [
    { id: 'r1', label: 'Good vibe', category: 'energy', phrasing: 'Someone felt your good vibe.', when: 'Today' },
    { id: 'r2', label: 'Warm smile', category: 'physical', phrasing: 'Someone noticed your warm smile.', when: 'Today' },
    { id: 'r3', label: 'Genuine', category: 'authentic', phrasing: 'Someone found you genuine.', when: 'Yesterday' },
    { id: 'r4', label: 'Calm presence', category: 'energy', phrasing: 'Someone felt calm around you.', when: '2 days ago' },
  ],
};

const FINGERPRINT = {
  headline: 'People meet you as warm and grounded.',
  summary:
    'Your fingerprint is how others tend to perceive you over time — phrased as perception, never as a score. It updates privately as more people notice you.',
  total: 34,
  needed: 20,
  // distribution across categories (shares sum ~1) — drives the radar
  distribution: [
    { category: 'physical', share: 0.24 },
    { category: 'energy', share: 0.34 },
    { category: 'style', share: 0.07 },
    { category: 'humor', share: 0.09 },
    { category: 'mind', share: 0.1 },
    { category: 'authentic', share: 0.16 },
  ],
  topTraits: [
    { label: 'Good vibe', category: 'energy', share: 0.26, phrasing: 'More than anything, people feel at ease around you.' },
    { label: 'Calm presence', category: 'energy', share: 0.21, phrasing: 'You tend to steady the room a little.' },
    { label: 'Warm smile', category: 'physical', share: 0.18, phrasing: 'Your smile is the thing people mention first.' },
    { label: 'Genuine', category: 'authentic', share: 0.15, phrasing: 'You read as real — not performing.' },
  ],
};

const INTERESTS = [
  'Pottery', 'Trail running', 'Vinyl', 'Surfing', 'Film photography', 'Cooking',
  'Synths', 'Cycling', 'Sci-fi', 'Climbing', 'Ceramics', 'Markets',
  'Chess', 'Jazz', 'Painting', 'Tea', 'Bouldering', 'Long walks',
];

// First appreciation a brand-new member receives (the emotional first-time moment).
const FIRST_APPRECIATION = {
  label: 'Warm smile', category: 'physical', hue: 38,
  phrasing: 'Someone just noticed your warm smile.',
  note: 'They didn’t say who — only what they appreciated.',
};

Object.assign(window, {
  CATEGORIES, CATEGORY_BY_ID, ALL_TRAITS, cat, catSoft, catTint, catInk,
  GENDER_GLYPH, PROFILES, RECEIVED, FINGERPRINT, INTERESTS, FIRST_APPRECIATION,
});
