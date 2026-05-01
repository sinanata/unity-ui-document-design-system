# Architecture

The design system is a stack of independent USS files imported by a single master stylesheet, plus one runtime helper script. This document walks through the layers and the load-bearing decisions behind them.

## The stack

```
DesignSystemRuntime.cs           ← C# helper, auto-attaches to UIDocument
        │
        ▼
DesignSystem.uss                 ← master, @imports the layers below
        │
        ├── DesignTokens.uss     ← :root variables only — no element rules
        ├── Typography.uss       ← .ds-h1 / .ds-h2 / .ds-h3 / .ds-body-1 / .ds-caption
        ├── Icons.uss            ← .ds-icon base + 63 .ds-icon--<name> + parent-state cascade
        ├── Buttons.uss          ← .ds-btn variants, sizes, icon button
        ├── Inputs.uss           ← .ds-input, .ds-search, .ds-dropdown, .ds-textarea
        ├── TabsAndFilters.uss   ← .ds-tabs, .ds-tab, .ds-view-toggle
        ├── Cards.uss            ← .ds-animal-card, .ds-info-row, .ds-swatch-row
        ├── Navigation.uss       ← .ds-side-nav, .ds-side-rail, .ds-bottom-nav, .ds-profile
        ├── Badges.uss           ← .ds-badge, .ds-tag, .ds-chip, .ds-avatar, .ds-notif-*
        ├── Controls.uss         ← .ds-toggle, .ds-check, .ds-radio, .ds-slider, .ds-range
        ├── Overlays.uss         ← .ds-modal, .ds-dialog, .ds-toast, .ds-sheet, .ds-empty
        ├── Feedback.uss         ← .ds-progress, .ds-spinner, .ds-skeleton, .ds-pagination
        └── Mobile.uss           ← .mobile-prefixed overrides — loaded LAST
```

A consumer attaches **one stylesheet** (`DesignSystem.uss`) to their `UIDocument`. The `@import` chain pulls everything else.

## Tokens

`DesignTokens.uss` declares CSS custom properties on `:root`. Every other file references them via `var(--…)`:

```css
/* DesignTokens.uss */
:root {
    --color-primary: #22C55E;
    --space-3: 12px;
}

/* Buttons.uss */
.ds-btn {
    background-color: var(--color-primary);
    padding-left: var(--space-3);
}
```

To re-theme: override `:root` in a higher-priority stylesheet attached after the design system:

```css
/* MyTheme.uss — attach after DesignSystem.uss on the same UIDocument */
:root {
    --color-primary: #FF6B35;     /* warm orange */
    --color-bg: #FAFAFA;          /* light theme */
    --color-text-primary: #111827;
}
```

No need to touch `Buttons.uss` or any other file. The cascade re-paints automatically.

### Per-axis radius tokens

Unity 6 USS clamps `border-radius` per axis to half the side length. A naive `border-radius: 999px` on a non-square element renders as an *ellipse*, not a CSS-style pill. The token set therefore ships explicit pill-radius values:

```css
--radius-pill-9:   9px;    /* 18×18 (toggle knob, slider thumb)         */
--radius-pill-12:  12px;   /* 24×24 (chip, tag, card check)             */
--radius-pill-16:  16px;   /* 32×32 (spinner, profile avatar)           */
--radius-pill-20:  20px;   /* 40×40 (avatar-md)                         */
...
```

Each token equals **half the height of an element it's designed for**. A 24-px chip uses `--radius-pill-12`; a 40-px avatar uses `--radius-pill-20`. Picking the right token is your responsibility — the comment on each token names its intended consumers.

## Why the import order matters

USS specificity ties resolve by source order. When two rules with the same specificity target the same element, the rule loaded *later* wins. The system uses this deliberately:

```css
/* Icons.uss — loaded 3rd */
.ds-icon { width: 20px; height: 20px; }

/* Inputs.uss — loaded 5th, AFTER Icons.uss */
.ds-search__icon { width: 18px; height: 18px; }
```

A `<VisualElement class="ds-icon ds-icon--search ds-search__icon">` element gets the **18×18** size from `.ds-search__icon`, not the 20×20 from `.ds-icon`, because Inputs.uss loads later.

This is the pattern by which "general icon" rules are specialised by per-component slot rules without writing higher-specificity selectors.

`Mobile.uss` is intentionally loaded **last** so its `.mobile`-prefixed overrides always win. If you reorder the imports, the responsive pass breaks first.

## Parent-state cascade for icons

A common request: "an icon inside a hovered button should retint." The naive solution is per-component `:hover .icon` rules, multiplied across every consumer. The system instead ships **one** cascade in `Icons.uss`:

```css
.ds-btn:hover .ds-icon,
.ds-tab:hover .ds-icon,
.ds-nav-item:hover .ds-icon,
.ds-rail-item:hover .ds-icon,
.ds-bottom-nav__item:hover .ds-icon,
.ds-pagination__btn:hover .ds-icon,
.ds-stepper__btn:hover .ds-icon {
    -unity-background-image-tint-color: var(--color-text-primary);
}
```

A new interactive container can opt into the cascade by adding its selector to the list. The icon picks up hover / active / pressed / disabled tints automatically, no per-consumer rule needed.

Filled-background controls (primary / secondary / tertiary / danger buttons, active tabs) override the cascade with on-accent ink so the glyph stays legible against the bg fill:

```css
.ds-btn--primary .ds-icon,
.ds-btn--secondary .ds-icon,
.ds-btn--tertiary .ds-icon,
.ds-btn--danger .ds-icon {
    -unity-background-image-tint-color: var(--color-text-on-accent);
}
```

Soft-bg controls (`.ds-nav-item.is-active`, `.ds-rail-item.is-active`, `.ds-bottom-nav__item.is-active`) keep their primary-tinted glyph via per-`__icon` rules in their own files. The cascade list intentionally does *not* include these, to avoid double-overriding what their own file specifies.

## The runtime layer

`DesignSystemRuntime.cs` is auto-attached via two hooks:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
static void RegisterAutoAttach() {
    SceneManager.sceneLoaded += OnSceneLoaded;
}

static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
    AttachToAllUIDocuments();      // iterate, AddComponent if missing
}
```

Every `UIDocument` in every scene gets a `DesignSystemRuntime` MonoBehaviour. The runtime then:

1. **Injects toggle knobs.** Unity's `Toggle` element doesn't render the iOS-style sliding pill — its checkmark is a square box. The runtime queries `.ds-toggle .unity-toggle__input` and adds a `.ds-toggle__knob` child if one isn't already present. Idempotent. Runs once at attach time and re-scans every 250 ms to cover toggles cloned in lazily by screen managers (e.g. a Settings panel that creates its toggles on first open).
2. **Drives spinner rotation.** USS `transition` can't loop natively. The runtime increments `transform.rotate` by 6° every frame on `.ds-spinner.is-spinning` elements.
3. **Animates skeleton shimmer.** `.ds-skeleton` placeholders get a child `.ds-skeleton__shimmer` element which slides via `translate` from -100 % to +100 % across a 1.4 s loop.

Two helpers are exposed `public static` so screen managers can call them eagerly after a template clone:

```csharp
DesignSystemRuntime.EnsureToggleKnobs(root);
DesignSystemRuntime.EnsureSkeletonShimmers(root);
```

This avoids the one-frame "flat pill" flash on the very first appearance of a screen, while the periodic re-scan still picks up anything cloned later.

## What lives in C# vs USS

The boundary is intentional:

| Concern | Lives in |
| --- | --- |
| Colours, spacing, sizing, radii, motion timing | USS (tokens) |
| Layout — flex, position, alignment | USS |
| State variants — hover / active / disabled / .is-active | USS pseudo-classes + state classes |
| State **transitions** that USS can express | USS `transition` |
| State **transitions** that USS can't loop or animate | C# (runtime) — spinner rotation, skeleton shimmer |
| DOM-shape requirements Unity can't author in UXML | C# (runtime) — toggle knob injection |
| Localised text, dynamic image sources, click handlers | Consumer C# (your screen manager) |

If you find yourself writing C# to set a colour, the colour belongs in a token. If you find yourself writing USS for a click handler, you've taken a wrong turn.

## Mobile pattern

`.mobile` is a single class added to your screen root. Every responsive rule lives in `Mobile.uss` and is prefixed with `.mobile`:

```css
/* Buttons.uss */
.ds-btn { height: 36px; }

/* Mobile.uss */
.mobile .ds-btn { height: 48px; }     /* same selector, .mobile prefix */
```

The selector `.mobile .ds-btn` has higher specificity (`0,2,0`) than `.ds-btn` (`0,1,0`), so the mobile rule always wins when the screen root carries `.mobile`.

Pattern across the system:

- Buttons / inputs / tabs grow to 48 px tall (touch target minimum).
- Slider thumbs grow 18 px → 24 px with recomputed `margin-top: -12px` for centring.
- Modals widen, side rails compact, bottom-nav bar takes over from side-nav.

You toggle the class once at screen build time:

```csharp
if (PanelSettingsHelper.IsMobileLayout())   // your own platform check
    root.AddToClassList("mobile");
```

Same UXML, same component classes — the layout flips.

## Adding a new layer

If a new component family doesn't fit `Cards.uss` or `Overlays.uss`:

1. Create `<Family>.uss` next to the existing layer files.
2. Use only `var(--…)` token references.
3. Append to `DesignSystem.uss`'s `@import` chain — typically before `Mobile.uss`.
4. Add `.mobile` overrides for the new family in `Mobile.uss`.

The `@import` order is the single source of truth for cascade. Don't try to control order via specificity tricks; let the import sequence carry it.
