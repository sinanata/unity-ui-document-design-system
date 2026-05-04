using UnityEngine;
using UnityEngine.UIElements;

namespace UIDocumentDesignSystem.Showcase
{
    // Spawns the showcase + doc-overlay UIDocuments at runtime so the .unity
    // scene stays empty (one camera). Means the scene file has no MonoBehaviour
    // GUID references that could rot during refactors — the whole stack is
    // recreated programmatically every Play.
    public static class ShowcaseBootstrap
    {
        const string SHOWCASE_RES_PATH = "UI/Styles/DesignSystem/DesignSystemShowcase";
        const string THEME_RES_PATH    = "UnityDefaultRuntimeTheme";
        const int    MOBILE_BREAKPOINT = 768;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            var showcaseUxml = Resources.Load<VisualTreeAsset>(SHOWCASE_RES_PATH);
            if (showcaseUxml == null)
            {
                Debug.LogError($"[ShowcaseBootstrap] Could not load {SHOWCASE_RES_PATH}.uxml from Resources. " +
                               "Confirm Assets/DesignSystem/Resources/UI/Styles/DesignSystem/DesignSystemShowcase.uxml exists.");
                return;
            }

            // The PanelSettings need a Theme Style Sheet for default Unity
            // control styling (Label fonts, Button frames, Toggle frames).
            // Without it Unity logs "No Theme Style Sheet set" and most text
            // renders invisible. The TSS at Resources/ just imports
            // unity-theme://default — same as the file Unity auto-creates
            // the first time you make a PanelSettings asset in the editor.
            var theme = Resources.Load<ThemeStyleSheet>(THEME_RES_PATH);
            if (theme == null)
            {
                Debug.LogWarning($"[ShowcaseBootstrap] Could not load {THEME_RES_PATH}.tss from Resources. " +
                                 "Default control styling will be missing. " +
                                 "Confirm Assets/Showcase/Resources/UnityDefaultRuntimeTheme.tss exists.");
            }

            var showcaseGO = new GameObject("Showcase");
            var showcaseDoc = showcaseGO.AddComponent<UIDocument>();
            showcaseDoc.panelSettings = MakePanelSettings(sortingOrder: 0, name: "ShowcasePanelSettings", theme: theme);
            showcaseDoc.visualTreeAsset = showcaseUxml;

            // Showcase-only override stylesheet — adds the universal
            // colour-property transition + .theme-light token block. Loaded
            // AFTER the design system stylesheet (which the UXML imports
            // first) so its rules win specificity ties.
            var themeOverride = Resources.Load<StyleSheet>("ShowcaseTheme");
            if (themeOverride != null && showcaseDoc.rootVisualElement != null)
                showcaseDoc.rootVisualElement.styleSheets.Add(themeOverride);

            // Mobile flip + promo-button wiring + theme-toggle wiring —
            // deferred one frame because rootVisualElement isn't built
            // until UIDocument has had its first OnEnable pass.
            showcaseDoc.rootVisualElement?.schedule.Execute(() =>
            {
                var root = showcaseDoc.rootVisualElement;
                ApplyMobileClass(root);
                WirePromoLinks(root);
                WireThemeToggle(root);
            }).StartingIn(0);

            var overlayGO = new GameObject("ShowcaseDocOverlay");
            var overlayDoc = overlayGO.AddComponent<UIDocument>();
            overlayDoc.panelSettings = MakePanelSettings(sortingOrder: 1, name: "DocOverlayPanelSettings", theme: theme);

            var overlay = overlayGO.AddComponent<ShowcaseDocOverlay>();
            overlay.AttachTo(showcaseDoc, overlayDoc);

            // The DesignSystemRuntime auto-attaches via SceneManager.sceneLoaded
            // which may fire BEFORE our AfterSceneLoad init — so the GameObjects
            // we just created would miss the initial attach. Nudge it manually.
            // The runtime is idempotent; calling twice is a no-op.
            UIDocumentDesignSystem.DesignSystemRuntime.AttachToAllUIDocuments();
        }

        static PanelSettings MakePanelSettings(int sortingOrder, string name, ThemeStyleSheet theme)
        {
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.name = name;
            if (theme != null) ps.themeStyleSheet = theme;

            // ConstantPixelSize so components render at their declared pixel
            // sizes regardless of viewport — what you see is what they ship
            // as in your own project. ScaleWithScreenSize would stretch the
            // whole UI to a reference resolution, which is right for a game
            // HUD but misleading for a design-system reference: a designer
            // hovering a `.ds-btn` wants to see a 36px button at 36px, not a
            // proportionally scaled version of one.
            //
            // The flex-wrap layout in the showcase UXML already reflows when
            // the viewport narrows, and `.mobile` (added by the bootstrap
            // when Screen.width < 768) flips spacing/touch-target tokens.
            // Together they cover every viewport without a global scale.
            ps.scaleMode = PanelScaleMode.ConstantPixelSize;
            ps.sortingOrder = sortingOrder;
            ps.targetDisplay = 0;
            ps.clearColor = sortingOrder == 0;
            ps.colorClearValue = new Color(0.043f, 0.058f, 0.090f, 1f); // --color-bg
            return ps;
        }

        static void ApplyMobileClass(VisualElement root)
        {
            if (root == null) return;
            bool mobile = Screen.width < MOBILE_BREAKPOINT;
            if (mobile && !root.ClassListContains("mobile")) root.AddToClassList("mobile");
            if (!mobile && root.ClassListContains("mobile")) root.RemoveFromClassList("mobile");
        }

        // Wire the promo-banner buttons in DesignSystemShowcase.uxml to real
        // URLs. Application.OpenURL works in the WebGL build — clicking
        // opens a new browser tab with the GitHub repo / Steam page.
        static void WirePromoLinks(VisualElement root)
        {
            if (root == null) return;
            var gh = root.Q<Button>("promo-github");
            if (gh != null) gh.clicked += () => Application.OpenURL("https://github.com/sinanata/unity-ui-document-design-system");
            var st = root.Q<Button>("promo-steam");
            if (st != null) st.clicked += () => Application.OpenURL("https://store.steampowered.com/app/2269500/");
        }

        // Hex pairs per swatch — first value is the dark-theme hex (matches
        // DesignTokens.uss), second is the light-theme hex (matches the
        // .theme-light block in Showcase/Resources/ShowcaseTheme.uss).
        // Keep in sync with both files when adjusting palettes.
        static readonly System.Collections.Generic.Dictionary<string, (string Dark, string Light)> SwatchHex =
            new System.Collections.Generic.Dictionary<string, (string, string)>
            {
                { "hex-primary",         ("#22C55E", "#16A34A") },
                { "hex-primary-hover",   ("#16A34A", "#15803D") },
                { "hex-secondary",       ("#3B82F6", "#2563EB") },
                { "hex-tertiary",        ("#A855F7", "#9333EA") },
                { "hex-warning",         ("#F59E0B", "#D97706") },
                { "hex-danger",          ("#EF4444", "#DC2626") },
                { "hex-text-primary",    ("#F2F4F7", "#0F172A") },
                { "hex-text-secondary",  ("#A1A7B3", "#475569") },
                { "hex-text-disabled",   ("#677085", "#94A3B8") },
                { "hex-bg",              ("#0B0F17", "#F8FAFC") },
                { "hex-surface",         ("#131A24", "#FFFFFF") },
                { "hex-surface-elev",    ("#1A2330", "#F1F5F9") },
                { "hex-border",          ("#263041", "#E2E8F0") },
            };

        // Wire the day/night toggle in the COLORS section header. Adds /
        // removes the `theme-light` class on .ds-root; ShowcaseTheme.uss
        // redefines every colour token under that class, the universal
        // transition rule animates the swap across the whole tree, and the
        // hex labels in the COLORS section are rewritten to match.
        static void WireThemeToggle(VisualElement root)
        {
            if (root == null) return;
            var toggle = root.Q<Toggle>("theme-toggle");
            if (toggle == null) return;
            toggle.RegisterValueChangedCallback(evt =>
            {
                bool light = evt.newValue;
                if (light) root.AddToClassList("theme-light");
                else       root.RemoveFromClassList("theme-light");
                UpdateHexLabels(root, light);
            });
        }

        static void UpdateHexLabels(VisualElement root, bool light)
        {
            foreach (var kv in SwatchHex)
            {
                var label = root.Q<Label>(kv.Key);
                if (label == null) continue;
                label.text = light ? kv.Value.Light : kv.Value.Dark;
            }
        }
    }
}
