# Changelog

All notable changes to this project will be documented here.

This project loosely follows [Semantic Versioning](https://semver.org/) and uses the [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format.

## [1.0.0] — 2026-05-01

Initial open-source release. The design system has been used in production by [Leap of Legends](https://leapoflegends.com) since early 2026; this is the first cut packaged for general consumption.

### Added

- **Design tokens** (`DesignTokens.uss`). Dark-themed palette: primary green, secondary blue, tertiary purple, warning amber, danger red, plus surface / border / overlay neutrals. Per-axis pill-radius tokens (`--radius-pill-9` … `--radius-pill-36`) so circles stay circles regardless of element height — Unity 6 clamps `border-radius` per axis to half the side, so a one-token-fits-all approach renders ellipses on rectangular elements.
- **Typography scale** (`Typography.uss`). `.ds-h1` (26 px / bold) → `.ds-caption` (11 px / medium). Inherits Unity 6's default Inter font; override via `--unity-font-definition` on the screen root.
- **Icon system** (`Icons.uss` + 63 SVGs). White-fill SVGs imported as `Texture2D` (`svgType: 3`); USS `resource(...)` resolves the artwork; `-unity-background-image-tint-color` paints it via the design-system token palette. Parent-state cascades retint icons inside `.ds-btn:hover`, `.ds-tab.is-active`, etc., automatically — no per-consumer `:hover .icon` rules needed.
- **Buttons** (`Buttons.uss`). 5 variants — primary / secondary / tertiary / ghost / danger. 4 states each — default / hover / pressed / disabled. Sizes `--sm` (28 px), default (36 px), `--lg` (44 px). Modifier `--block` for full-width. Square `--icon` variant for icon-only buttons.
- **Inputs** (`Inputs.uss`). `.ds-input` text field, `.ds-search` shell with leading-icon slot, `.ds-dropdown` for `<DropdownField>`, `.ds-textarea` with optional counter slot. All four target every Unity 6 inner-class variant (`.unity-text-input`, `.unity-text-field__input`, `.unity-base-text-field__input`, `.unity-base-field__input`) so the visual shell paints regardless of which name Unity emits for the field type.
- **Tabs & filters** (`TabsAndFilters.uss`). `.ds-tabs` segmented strip, `.ds-tab` with `.is-active` state, `.ds-view-toggle` for grid/list switching.
- **Controls** (`Controls.uss`). `.ds-toggle` iOS-style switch (knob auto-injected by the runtime), `.ds-check` square checkbox with shrunk tick to fit inside the 2 px border, `.ds-radio` styled radio button, `.ds-slider` single-value with cross-centred thumb, `.ds-range` MinMaxSlider with thumbs and dragger explicitly cross-centred via `top: 50%; margin-top: -<half>px;` (Unity stock layout floats them above the track).
- **Cards** (`Cards.uss`). Animal-card example (game shape, demonstrates layered children + `.is-selected` / `.is-epic` modifiers + check pin), `.ds-info-row` for two-column attribute lists, `.ds-swatch-row` for tokens display.
- **Navigation** (`Navigation.uss`). `.ds-side-nav` (full-width with labels), `.ds-side-rail` (icon-only), `.ds-bottom-nav` (mobile tab bar), `.ds-profile` chip with avatar slot.
- **Badges & labels** (`Badges.uss`). Rarity pills (common / rare / epic / legendary), habitat tags, status chips (equipped / new / owned / limited / event / sale) with per-variant icon tints, notification dot (single-digit circle and multi-digit pill modes), avatar in 4 sizes.
- **Overlays** (`Overlays.uss`). `.ds-modal` with header / body / actions slots, `.ds-dialog` for confirms, `.ds-toast` in success / info / warning / danger flavours, `.ds-sheet` mobile bottom drawer, `.ds-empty` empty-state.
- **Feedback** (`Feedback.uss`). `.ds-progress` bar, `.ds-spinner` circular loader (driven by the runtime — USS transitions can't loop), `.ds-skeleton` placeholder card with shimmering overlay, `.ds-pagination`, `.ds-stepper` quantity selector.
- **Mobile responsiveness** (`Mobile.uss`). One class on the screen root (`.mobile`) flips every spacing token, tap target, and dropdown to touch-friendly sizes. Same UXML, same components — two layouts. Buttons 36 → 48 px, inputs 40 → 48 px, slider thumbs 18 → 24 px with recomputed `margin-top` for centring, tabs grow, modals widen.
- **`DesignSystemRuntime.cs`** auto-attaches to every `UIDocument` in a scene via `RuntimeInitializeOnLoadMethod` + `SceneManager.sceneLoaded`. Provides:
    - **Toggle knob auto-injection** — Unity's `Toggle` element doesn't render the iOS-style sliding pill on its own. The runtime injects a child `.ds-toggle__knob` into `.unity-toggle__input` every 250 ms (idempotent, cheap), so toggles cloned in lazily by screen managers (Settings panels etc.) get the knob without per-screen wiring.
    - **Spinner rotation** — increments transform rotate by 6° per frame on `.ds-spinner.is-spinning` elements. USS `transition` can't loop natively.
    - **Skeleton shimmer** — animates the `.ds-skeleton__shimmer` child via `translate` for the loading-state placeholder.
- **Showcase UXML** (`DesignSystemShowcase.uxml`) — a single scrollable view that renders every component, every state, every icon. Drop it on a `UIDocument`, hit Play, and you have a living style guide. 22 sections: Colors, Typography, Buttons, Inputs, Tabs & Filters, Animal Card, Animal Detail, Navigation, Badges & Labels, Icons, Toggles & Checks, Sliders, Modals / Panels, Toasts, Empty States, Bottom Sheet, Confirm Dialog, Quantity, Pagination, Loading States, Notification Badge, Avatar.

### Documentation

- `README.md` — top-level overview, install, quick start, mobile pattern, icon authoring.
- `docs/ARCHITECTURE.md` — token hierarchy, USS load order, runtime auto-attach lifecycle, parent-state cascade rules.
- `docs/COMPONENTS.md` — one-line reference per component class, with the expected child structure.
- `docs/ICONS.md` — adding new icons, the white-fill convention, how the parent-state tint cascade works, when to use `.ds-icon--accent` vs letting the parent retint.
- `docs/MOBILE.md` — the `.mobile` override pattern, when to add the class, what it flips, how to extend.
- `CONTRIBUTING.md` — naming convention (`ds-` prefix, BEM-ish modifiers), file-load order, PR checklist.

### Known caveats / what isn't here yet

- **Light theme.** The token structure supports it (just override `:root` from a higher-priority stylesheet) but a polished `LightTokens.uss` is on the roadmap, not in 1.0.
- **RTL.** Layout flips for arrow / chevron icons and side-nav direction haven't been authored. The token system is RTL-neutral; the UXML structure isn't.
- **No editor scripts.** Live `.mobile` toggling and class-list inspection are user-side for now. PRs welcome.
