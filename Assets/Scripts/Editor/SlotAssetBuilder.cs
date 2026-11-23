using System.Collections.Generic;
using System.Linq;
using SlotMachine.Core;
using SlotMachine.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using static SlotMachine.EditorTools.BuilderUtility;

namespace SlotMachine.EditorTools
{
    /// <summary>
    /// Prepares every asset the scene needs: texture import settings and
    /// sprite-sheet slicing, pixel-font assets, the screen-effect material,
    /// the symbol / machine data, and the Animator clips and controllers.
    /// </summary>
    internal static class SlotAssetBuilder
    {
        public const string UiRoot = "Assets/UI/";
        public const string Machine = UiRoot + "Machine/";
        public const string Symbols = UiRoot + "Symbols/";
        public const string Buttons = UiRoot + "Buttons/";
        public const string Panels = UiRoot + "Panels/";
        public const string Fonts = UiRoot + "Fonts/";
        public const string Animations = "Assets/Animations/";
        public const string Data = "Assets/Data/";

        public const string HeadingFontPath = Fonts + "PressStart2P SDF.asset";
        public const string BodyFontPath = Fonts + "VT323 SDF.asset";
        public const string ScreenMaterialPath = "Assets/Shaders/RetroUpscale.mat";
        public const string ConfigPath = Data + "SlotMachineConfig.asset";

        public static void BuildAll()
        {
            ConfigureTextures();
            BuildFonts();
            BuildScreenMaterial();
            BuildData();
            BuildAnimations();
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------- textures

        private static void ConfigureTextures()
        {
            // Pixel art drawn at its native size: hard nearest-neighbour pixels.
            foreach (var path in new[]
            {
                Machine + "machine_frame.png", Machine + "machine_reel_windows.png", Machine + "machine_full.png",
                Machine + "lever_up.png", Machine + "lever_down.png", Machine + "jackpot_sign_glow.png",
                Machine + "bulb.png", Machine + "pixel_white.png",
            })
                ConfigureSprite(path, FilterMode.Point, mipmaps: false);

            ConfigureSprite(Machine + "frame_glow.png", FilterMode.Point, mipmaps: false, border: new Vector4(3, 3, 3, 3));

            // Symbols are shown 1:1 on the reels and at half size in the paytable;
            // mipmaps with point filtering keep both crisp.
            foreach (var name in new[] { "seven", "cherry", "bell", "bar" })
            {
                ConfigureSprite(Symbols + "symbol_" + name + ".png", FilterMode.Point, mipmaps: true);
                ConfigureSprite(Symbols + "Blur/symbol_" + name + "_blur.png", FilterMode.Point, mipmaps: true);
            }

            // Painted UI art is shown well below its source size, so it is
            // filtered smoothly; the retro screen effect pixelates it afterwards.
            ConfigureSprite(UiRoot + "Backgrounds/bg_gradient.png", FilterMode.Bilinear, mipmaps: true);
            ConfigureSprite(Panels + "popup_panel.png", FilterMode.Bilinear, mipmaps: true, border: new Vector4(120, 110, 120, 110));
            ConfigureSprite(Panels + "middle_box.png", FilterMode.Bilinear, mipmaps: true, border: new Vector4(70, 60, 70, 60));

            // Button sheets: four states stacked top to bottom (normal,
            // highlighted, pressed, disabled), each 256x256.
            SliceStates(Buttons + "button_close_sheet.png", "close");
            SliceStates(Buttons + "button_arrow_right_sheet.png", "arrow_right");
            SliceStates(Buttons + "button_arrow_left_sheet.png", "arrow_left");

            // Blank gold buttons made from the YES/NO art by Tools/generate_retro_assets.py,
            // in the same four-state layout; labels and icons are drawn on top.
            SliceStates(Buttons + "button_gold_wide_sheet.png", "gold_wide", 371, 142);
            SliceStates(Buttons + "button_gold_square_sheet.png", "gold_square", 142, 142);

            // 16x16 pixel icons for the sound and filter toggles, kept hard-edged.
            string[] icons = { "icon_sound_on", "icon_sound_off", "icon_filter_on", "icon_filter_off" };
            Slice(Buttons + "icons_toggle_sheet.png",
                icons.Select((name, i) => (name, new Rect(i * 16, 0, 16, 16))).ToList(), FilterMode.Point, mipmaps: false);

            // YES / NO sheet: two columns, three state rows (normal, highlighted, pressed).
            var yesNo = new List<(string, Rect)>();
            string[] states = { "normal", "highlight", "pressed" };
            for (int row = 0; row < 3; row++)
            {
                float y = 690 - (row + 1) * 230; // sprite rects use a bottom-left origin
                yesNo.Add(("yes_" + states[row], new Rect(0, y, 495, 230)));
                yesNo.Add(("no_" + states[row], new Rect(495, y, 495, 230)));
            }
            Slice(Buttons + "button_yes_no_sheet.png", yesNo);
        }

        private static void ConfigureSprite(string path, FilterMode filter, bool mipmaps, Vector4? border = null)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = filter;
            importer.mipmapEnabled = mipmaps;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100;
            importer.maxTextureSize = 2048;
            if (border.HasValue) importer.spriteBorder = border.Value;
            importer.SaveAndReimport();
        }

        /// <summary>Slices a sheet of four button states stacked top to bottom, each width x height.</summary>
        private static void SliceStates(string path, string prefix, int width = 256, int height = 256)
        {
            string[] states = { "normal", "highlight", "pressed", "disabled" };
            var cells = new List<(string, Rect)>();
            for (int row = 0; row < 4; row++)
                cells.Add((prefix + "_" + states[row], new Rect(0, (3 - row) * height, width, height)));
            Slice(path, cells);
        }

        private static void Slice(string path, IList<(string name, Rect rect)> cells,
            FilterMode filter = FilterMode.Bilinear, bool mipmaps = true)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = filter;
            importer.mipmapEnabled = mipmaps;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var existing = provider.GetSpriteRects();
            var rects = cells.Select(c =>
            {
                var previous = existing.FirstOrDefault(e => e.name == c.name);
                return new SpriteRect
                {
                    name = c.name,
                    rect = c.rect,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = previous != null ? previous.spriteID : GUID.Generate(),
                };
            }).ToArray();

            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            provider.Apply();
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------- fonts

        private const string Charset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        private static void BuildFonts()
        {
            BuildFont(Fonts + "PressStart2P-Regular.ttf", HeadingFontPath, samplingSize: 32);
            BuildFont(Fonts + "VT323-Regular.ttf", BodyFontPath, samplingSize: 48);
        }

        private static void BuildFont(string ttfPath, string assetPath, int samplingSize)
        {
            DeleteIfExists(assetPath);
            var font = Load<Font>(ttfPath);
            var asset = TMP_FontAsset.CreateFontAsset(font, samplingSize, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);
            asset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);

            // The atlas and material must live inside the font asset to be saved.
            asset.atlasTextures[0].name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            if (!asset.TryAddCharacters(Charset, out string missing) && !string.IsNullOrEmpty(missing))
                Debug.LogWarning(asset.name + " is missing glyphs: " + missing);

            // Freeze the atlas: the game only uses these characters, and a static
            // atlas doesn't need the source font at runtime.
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(asset);
        }

        // ---------------------------------------------------- screen material

        private static void BuildScreenMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(ScreenMaterialPath) != null) return;
            var shader = Shader.Find("Hidden/SlotMachine/RetroUpscale");
            AssetDatabase.CreateAsset(new Material(shader) { name = "RetroUpscale" }, ScreenMaterialPath);
        }

        // -------------------------------------------------------------- data

        private static void BuildData()
        {
            var symbols = new[]
            {
                MakeSymbol(SymbolType.Seven, "Seven", "seven", printed: 2, multiplier: 50, freeSpins: 0),
                MakeSymbol(SymbolType.Bar, "Bar", "bar", printed: 4, multiplier: 25, freeSpins: 0),
                MakeSymbol(SymbolType.Bell, "Bell", "bell", printed: 6, multiplier: 10, freeSpins: 3),
                MakeSymbol(SymbolType.Cherry, "Cherry", "cherry", printed: 8, multiplier: 5, freeSpins: 0),
            };

            DeleteIfExists(ConfigPath);
            var config = ScriptableObject.CreateInstance<SlotMachineConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            SetArray(config, "symbols", symbols);
        }

        private static SymbolDefinition MakeSymbol(SymbolType type, string displayName, string art,
            int printed, int multiplier, int freeSpins)
        {
            string path = Data + "Symbol_" + displayName + ".asset";
            DeleteIfExists(path);
            var symbol = ScriptableObject.CreateInstance<SymbolDefinition>();
            AssetDatabase.CreateAsset(symbol, path);
            SetEnum(symbol, "type", (int)type);
            Set(symbol, "displayName", displayName);
            Set(symbol, "sprite", LoadSprite(Symbols + "symbol_" + art + ".png"));
            Set(symbol, "blurredSprite", LoadSprite(Symbols + "Blur/symbol_" + art + "_blur.png"));
            Set(symbol, "printedPerReel", printed);
            Set(symbol, "multiplier", multiplier);
            Set(symbol, "freeSpins", freeSpins);
            return symbol;
        }

        // -------------------------------------------------------- animations

        private static void BuildAnimations()
        {
            BuildLeverAnimations();
            BuildPopupAnimations();
            BuildJackpotSignAnimations();
        }

        /// <summary>Lever pull: swap to the pulled-down sprite, hold, spring back.</summary>
        private static void BuildLeverAnimations()
        {
            var up = LoadSprite(Machine + "lever_up.png");
            var down = LoadSprite(Machine + "lever_down.png");

            var idle = SpriteClip("Lever_Idle", (0f, up));
            var pull = SpriteClip("Lever_Pull", (0f, up), (0.05f, down), (0.32f, down), (0.38f, up));

            var controller = NewController("Lever");
            controller.AddParameter("Pull", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var idleState = AddState(machine, "Idle", idle, isDefault: true);
            var pullState = AddState(machine, "Pull", pull);
            Trigger(idleState, pullState, "Pull");
            Trigger(pullState, pullState, "Pull"); // re-pull mid-animation restarts it
            ExitTo(pullState, idleState);
        }

        /// <summary>Popup: fade the backdrop in and pop the panel slightly past full size.</summary>
        private static void BuildPopupAnimations()
        {
            var hidden = NewClip("Popup_Hidden");
            Curve(hidden, "", typeof(CanvasGroup), "m_Alpha", (0f, 0f));
            Scale(hidden, "Panel", (0f, 0.85f));

            var show = NewClip("Popup_Show");
            Curve(show, "", typeof(CanvasGroup), "m_Alpha", (0f, 0f), (0.12f, 1f));
            Scale(show, "Panel", (0f, 0.85f), (0.14f, 1.06f), (0.22f, 1f));

            var hide = NewClip("Popup_Hide");
            Curve(hide, "", typeof(CanvasGroup), "m_Alpha", (0f, 1f), (0.14f, 0f));
            Scale(hide, "Panel", (0f, 1f), (0.14f, 0.9f));

            var controller = NewController("Popup");
            controller.AddParameter("Show", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hide", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var hiddenState = AddState(machine, "Hidden", hidden, isDefault: true);
            var showState = AddState(machine, "Shown", show);
            var hideState = AddState(machine, "Hiding", hide);
            Trigger(hiddenState, showState, "Show");
            Trigger(hideState, showState, "Show");
            Trigger(showState, hideState, "Hide");
            ExitTo(hideState, hiddenState);
        }

        /// <summary>Jackpot sign: the letters blink on and off in hard steps, like marquee bulbs.</summary>
        private static void BuildJackpotSignAnimations()
        {
            var idle = NewClip("JackpotSign_Idle");
            Curve(idle, "", typeof(Image), "m_Color.a", (0f, 0f));

            var flash = NewClip("JackpotSign_Flash");
            var keys = new List<(float, float)>();
            for (int i = 0; i <= 16; i++) keys.Add((i * 0.12f, i % 2 == 0 ? 1f : 0f));
            Curve(flash, "", typeof(Image), "m_Color.a", stepped: true, keys.ToArray());

            var controller = NewController("JackpotSign");
            controller.AddParameter("Flash", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var idleState = AddState(machine, "Idle", idle, isDefault: true);
            var flashState = AddState(machine, "Flash", flash);
            Trigger(idleState, flashState, "Flash");
            ExitTo(flashState, idleState);
        }

        private static AnimationClip NewClip(string name)
        {
            string path = Animations + name + ".anim";
            DeleteIfExists(path);
            var clip = new AnimationClip { name = name, frameRate = 60 };
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static AnimationClip SpriteClip(string name, params (float time, Sprite sprite)[] keys)
        {
            var clip = NewClip(name);
            var binding = EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding,
                keys.Select(k => new ObjectReferenceKeyframe { time = k.time, value = k.sprite }).ToArray());
            return clip;
        }

        private static void Curve(AnimationClip clip, string path, System.Type type, string property,
            params (float time, float value)[] keys) => Curve(clip, path, type, property, false, keys);

        private static void Curve(AnimationClip clip, string path, System.Type type, string property,
            bool stepped, params (float time, float value)[] keys)
        {
            var curve = new AnimationCurve(keys.Select(k => new Keyframe(k.time, k.value)).ToArray());
            if (stepped)
            {
                for (int i = 0; i < curve.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                }
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), curve);
        }

        private static void Scale(AnimationClip clip, string path, params (float time, float value)[] keys)
        {
            Curve(clip, path, typeof(RectTransform), "m_LocalScale.x", keys);
            Curve(clip, path, typeof(RectTransform), "m_LocalScale.y", keys);
            Curve(clip, path, typeof(RectTransform), "m_LocalScale.z", keys.Select(k => (k.time, 1f)).ToArray());
        }

        private static AnimatorController NewController(string name)
        {
            string path = Animations + name + ".controller";
            DeleteIfExists(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, bool isDefault = false)
        {
            var state = machine.AddState(name);
            state.motion = motion;
            state.writeDefaultValues = true;
            if (isDefault) machine.defaultState = state;
            return state;
        }

        private static void Trigger(AnimatorState from, AnimatorState to, string parameter)
        {
            var t = from.AddTransition(to);
            t.AddCondition(AnimatorConditionMode.If, 0f, parameter);
            t.hasExitTime = false;
            t.duration = 0f;
        }

        private static void ExitTo(AnimatorState from, AnimatorState to)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = 1f;
            t.duration = 0f;
        }
    }
}
