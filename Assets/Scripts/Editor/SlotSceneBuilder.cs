using System.Collections.Generic;
using System.Linq;
using SlotMachine.Audio;
using SlotMachine.Effects;
using SlotMachine.Gameplay;
using SlotMachine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static SlotMachine.EditorTools.BuilderUtility;
using static SlotMachine.EditorTools.SlotAssetBuilder;

namespace SlotMachine.EditorTools
{
    /// <summary>
    /// Builds the prefabs and the playable scene. The layout uses a 1280x720
    /// reference canvas so the 816x624 machine art sits at exactly 1:1 pixels.
    /// </summary>
    internal static class SlotSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/SlotMachine.unity";
        private const string Prefabs = "Assets/Prefabs/";

        // Palette
        private static readonly Color Gold = new Color(1f, 0.84f, 0.32f);
        private static readonly Color Lavender = new Color(0.78f, 0.8f, 1f);
        private static readonly Color Mint = new Color(0.55f, 1f, 0.75f);
        private static readonly Color Night = new Color32(11, 8, 28, 255);

        private static TMP_FontAsset heading;
        private static TMP_FontAsset body;

        // Reel windows measured on the machine art (x, y, width, height; top-left origin).
        private static readonly Rect[] ReelWindows =
        {
            new Rect(229, 246, 108, 210), new Rect(359, 246, 108, 210), new Rect(489, 246, 108, 210)
        };

        public static void BuildAll()
        {
            heading = Load<TMP_FontAsset>(HeadingFontPath);
            body = Load<TMP_FontAsset>(BodyFontPath);

            var reelPrefab = BuildReelPrefab();
            var infoBoxPrefab = BuildInfoBoxPrefab();
            var rowPrefab = BuildPaytableRowPrefab();
            var popupPrefab = BuildPopupPrefab();
            BuildScene(reelPrefab, infoBoxPrefab, rowPrefab, popupPrefab);
        }

        // =========================================================== prefabs

        /// <summary>One masked reel window with five recycled symbol cells.</summary>
        private static GameObject BuildReelPrefab()
        {
            var root = Rect("Reel", null, new Vector2(108, 210), Vector2.zero);
            root.gameObject.AddComponent<RectMask2D>();
            var reel = root.gameObject.AddComponent<ReelView>();

            var cherry = LoadSprite(Symbols + "symbol_cherry.png");
            var cells = new Image[5];
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = Rect("Cell_" + i, root, new Vector2(96, 96), new Vector2(0, -105 + (i - 2) * 96),
                    anchor: new Vector2(0.5f, 1f));
                cells[i] = AddImage(cell, cherry);
            }
            SetArray(reel, "cells", cells);
            return SavePrefab(root.gameObject, "Reel");
        }

        /// <summary>A labelled readout on the provided "middle box" panel.</summary>
        private static GameObject BuildInfoBoxPrefab()
        {
            var root = Rect("InfoBox", null, new Vector2(280, 118), Vector2.zero);
            AddImage(root, LoadSprite(Panels + "middle_box.png"), type: Image.Type.Sliced, pixelsPerUnitMultiplier: 2.35f);
            AddText("Label", root, heading, "LABEL", 10, Lavender, new Vector2(240, 16), new Vector2(0, 25));
            AddText("Value", root, heading, "0", 24, Color.white, new Vector2(250, 32), new Vector2(0, -12));
            return SavePrefab(root.gameObject, "InfoBox");
        }

        /// <summary>A paytable line: three half-size symbols, the payout and a note.</summary>
        private static GameObject BuildPaytableRowPrefab()
        {
            var root = Rect("PaytableRow", null, new Vector2(248, 50), Vector2.zero);
            var row = root.gameObject.AddComponent<PaytableRow>();
            var sprite = LoadSprite(Symbols + "symbol_cherry.png");
            var icons = new Image[3];
            for (int i = 0; i < 3; i++)
                icons[i] = AddImage(Rect("Icon_" + i, root, new Vector2(48, 48), new Vector2(-100 + i * 44, 0)), sprite);
            var payout = AddText("Payout", root, heading, "x5", 14, Gold, new Vector2(100, 18), new Vector2(72, 9), TextAlignmentOptions.Left);
            var note = AddText("Note", root, body, "NOTE", 20, Mint, new Vector2(100, 18), new Vector2(72, -12), TextAlignmentOptions.Left);
            SetArray(row, "icons", icons);
            Set(row, "payout", payout);
            Set(row, "note", note);
            return SavePrefab(root.gameObject, "PaytableRow");
        }

        /// <summary>The reusable popup: dimmed backdrop, panel art, title, body, X / YES / NO buttons.</summary>
        private static GameObject BuildPopupPrefab()
        {
            var root = Rect("Popup", null, new Vector2(1280, 720), Vector2.zero);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            var animator = root.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = Load<AnimatorController>(Animations + "Popup.controller");
            var view = root.gameObject.AddComponent<PopupView>();

            AddImage(Rect("Dim", root, new Vector2(1600, 900), Vector2.zero), LoadSprite(Machine + "pixel_white.png"),
                new Color(0.02f, 0.01f, 0.08f, 0.7f), raycast: true);

            var panel = Rect("Panel", root, new Vector2(692, 349), Vector2.zero);
            AddImage(panel, LoadSprite(Panels + "popup_panel.png"), raycast: true);
            var title = AddText("Title", panel, heading, "TITLE", 26, Gold, new Vector2(600, 36), new Vector2(0, 96));
            var text = AddText("Body", panel, body, "BODY", 36, Color.white, new Vector2(600, 110), new Vector2(0, 8));
            text.lineSpacing = -10f;

            const string sheet = Buttons + "button_close_sheet.png";
            var close = AddSpriteButton(Rect("CloseButton", panel, new Vector2(48, 48), new Vector2(298, 124)),
                LoadSprite(sheet, "close_normal"), LoadSprite(sheet, "close_highlight"),
                LoadSprite(sheet, "close_pressed"), LoadSprite(sheet, "close_disabled"));

            const string yesNo = Buttons + "button_yes_no_sheet.png";
            var yes = AddSpriteButton(Rect("YesButton", panel, new Vector2(198, 92), new Vector2(-120, -104)),
                LoadSprite(yesNo, "yes_normal"), LoadSprite(yesNo, "yes_highlight"), LoadSprite(yesNo, "yes_pressed"), LoadSprite(yesNo, "yes_pressed"));
            var no = AddSpriteButton(Rect("NoButton", panel, new Vector2(198, 92), new Vector2(120, -104)),
                LoadSprite(yesNo, "no_normal"), LoadSprite(yesNo, "no_highlight"), LoadSprite(yesNo, "no_pressed"), LoadSprite(yesNo, "no_pressed"));

            Set(view, "group", group);
            Set(view, "animator", animator);
            Set(view, "title", title);
            Set(view, "body", text);
            Set(view, "closeButton", close);
            Set(view, "yesButton", yes);
            Set(view, "noButton", no);
            return SavePrefab(root.gameObject, "Popup");
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            string path = Prefabs + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject Instantiate(GameObject prefab, Transform parent, string name, Vector2 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            ((RectTransform)go.transform).anchoredPosition = position;
            return go;
        }

        // ============================================================= scene

        private static void BuildScene(GameObject reelPrefab, GameObject infoBoxPrefab, GameObject rowPrefab, GameObject popupPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera with the retro pixel effect.
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Night;
            camera.cullingMask = 1 << UILayer;
            camera.transform.position = new Vector3(0, 0, -10);
            cameraGo.AddComponent<AudioListener>();
            var retro = cameraGo.AddComponent<RetroScreenEffect>();
            Set(retro, "upscaleMaterial", Load<Material>(ScreenMaterialPath));

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var audioService = BuildAudio();

            // Canvas: 1280x720 reference so the machine art is 1:1 with the virtual pixel grid.
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            {
                layer = UILayer
            };
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var stage = Stretch("Stage", canvasGo.transform);
            var shake = stage.gameObject.AddComponent<ScreenShake>();
            Set(shake, "target", stage);

            AddImage(Rect("Background", stage, new Vector2(1280, 720), Vector2.zero), LoadSprite(UiRoot + "Backgrounds/bg_gradient.png"));
            var lights = BuildMarquee(stage);

            var machine = BuildMachine(stage, reelPrefab, out var reels, out var glows, out var payline, out var sign, out var lever);
            var hud = BuildHud(stage, infoBoxPrefab, audioService, out var paytable, rowPrefab);
            var soundToggle = BuildIconToggle(hud.transform, "SoundToggle", "SOUND", new Vector2(508, 306),
                "icon_sound_on", "icon_sound_off", audioService);
            var filterToggle = BuildIconToggle(hud.transform, "FilterToggle", "FILTER", new Vector2(564, 306),
                "icon_filter_on", "icon_filter_off", audioService);

            var popupGo = Instantiate(popupPrefab, canvasGo.transform, "Popup", Vector2.zero);
            var popup = popupGo.GetComponent<PopupView>();
            foreach (var button in popupGo.GetComponentsInChildren<Button>(true)) AddClick(button, audioService);

            // The controller that runs the game.
            var game = new GameObject("SlotMachine").AddComponent<SlotMachineController>();
            Set(game, "config", Load<SlotMachineConfig>(ConfigPath));
            SetArray(game, "reels", reels);
            SetArray(game, "reelGlows", glows);
            Set(game, "lever", lever);
            Set(game, "payline", payline);
            Set(game, "jackpotSign", sign);
            Set(game, "hud", hud);
            Set(game, "paytable", paytable);
            Set(game, "popup", popup);
            Set(game, "lights", lights);
            Set(game, "screenShake", shake);
            Set(game, "audioService", audioService);
            Set(game, "retroEffect", retro);
            Set(game, "soundToggle", soundToggle);
            Set(game, "filterToggle", filterToggle);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static AudioService BuildAudio()
        {
            var go = new GameObject("Audio");
            var effects = go.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            var loop = go.AddComponent<AudioSource>();
            loop.playOnAwake = false;
            loop.loop = true;

            var service = go.AddComponent<AudioService>();
            Set(service, "effects", effects);
            Set(service, "loop", loop);

            var clips = new (Sfx id, string file, float volume)[]
            {
                (Sfx.ReelTick, "reel_tick", 0.22f), (Sfx.ReelStop, "reel_stop", 0.6f), (Sfx.LeverPull, "lever_pull", 0.7f),
                (Sfx.ButtonClick, "button_click", 0.45f), (Sfx.CoinTick, "coin_tick", 0.3f), (Sfx.WinSmall, "win_small", 0.65f),
                (Sfx.WinBig, "win_big", 0.75f), (Sfx.Jackpot, "jackpot", 0.85f), (Sfx.FreeSpins, "free_spins", 0.7f),
                (Sfx.Anticipation, "anticipation", 0.4f), (Sfx.NoCredits, "no_credits", 0.6f), (Sfx.PopupOpen, "popup_open", 0.35f),
            };
            Edit(service, "clips", p =>
            {
                p.arraySize = clips.Length;
                for (int i = 0; i < clips.Length; i++)
                {
                    var element = p.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("id").enumValueIndex = (int)clips[i].id;
                    element.FindPropertyRelative("clip").objectReferenceValue = Load<AudioClip>("Assets/Sounds/" + clips[i].file + ".wav");
                    element.FindPropertyRelative("volume").floatValue = clips[i].volume;
                }
            });
            return service;
        }

        /// <summary>Casino bulbs along the top edge and down both sides of the screen.</summary>
        private static MarqueeLights BuildMarquee(Transform stage)
        {
            var root = Stretch("Marquee", stage);
            var lights = root.gameObject.AddComponent<MarqueeLights>();
            var sprite = LoadSprite(Machine + "bulb.png");
            var bulbs = new List<Image>();

            void Bulb(Vector2 position) =>
                bulbs.Add(AddImage(Rect("Bulb_" + bulbs.Count, root, new Vector2(16, 16), position), sprite));

            for (int x = -608; x <= 608; x += 32) Bulb(new Vector2(x, 344));       // top row, left to right
            for (int y = 312; y >= -344; y -= 32) Bulb(new Vector2(624, y));       // right side, downwards
            for (int y = -344; y <= 312; y += 32) Bulb(new Vector2(-624, y));      // left side, upwards
            SetArray(lights, "bulbs", bulbs.ToArray());
            return lights;
        }

        private static RectTransform BuildMachine(Transform stage, GameObject reelPrefab, out ReelView[] reels,
            out WindowGlow[] glows, out PaylineView payline, out JackpotSignView sign, out LeverView lever)
        {
            // 816x624 art at 1:1, centred on its visible cabinet and resting on the bottom edge.
            var machine = Rect("Machine", stage, new Vector2(816, 624), new Vector2(-43, -48));
            AddImage(Rect("ReelWindows", machine, new Vector2(816, 624), Vector2.zero), LoadSprite(Machine + "machine_reel_windows.png"));

            var white = LoadSprite(Machine + "pixel_white.png");

            // Gold glow behind the payline row; it shows around the symbols on a win.
            var (lineCentre, _) = MachineRect(229, 301, 368, 100);
            var glowRect = Rect("PaylineGlow", machine, new Vector2(368, 100), lineCentre);
            var glowImage = AddImage(glowRect, white, new Color(1f, 0.85f, 0.3f, 0f));
            payline = glowRect.gameObject.AddComponent<PaylineView>();
            Set(payline, "glow", glowImage);

            reels = new ReelView[ReelWindows.Length];
            for (int i = 0; i < ReelWindows.Length; i++)
            {
                var w = ReelWindows[i];
                var (position, _) = MachineRect(w.x, w.y, w.width, w.height);
                reels[i] = Instantiate(reelPrefab, machine, "Reel_" + (i + 1), position).GetComponent<ReelView>();
            }

            // Thin red payline marker across the windows (only visible through the glass).
            AddImage(Rect("PaylineMarker", machine, new Vector2(368, 2), lineCentre), white, new Color(1f, 0.25f, 0.25f, 0.55f));

            AddImage(Rect("Frame", machine, new Vector2(816, 624), Vector2.zero), LoadSprite(Machine + "machine_frame.png"));

            glows = new WindowGlow[ReelWindows.Length];
            var frameSprite = LoadSprite(Machine + "frame_glow.png");
            for (int i = 0; i < ReelWindows.Length; i++)
            {
                var w = ReelWindows[i];
                var (position, size) = MachineRect(w.x - 4, w.y - 4, w.width + 8, w.height + 8);
                var rt = Rect("ReelGlow_" + (i + 1), machine, size, position);
                AddImage(rt, frameSprite, new Color(1f, 0.82f, 0.25f), type: Image.Type.Sliced);
                glows[i] = rt.gameObject.AddComponent<WindowGlow>();
            }

            // The JACKPOT letters, lit up by an Animator on a jackpot.
            var signRect = Rect("JackpotSignGlow", machine, new Vector2(816, 624), Vector2.zero);
            AddImage(signRect, LoadSprite(Machine + "jackpot_sign_glow.png"), new Color(1f, 1f, 1f, 0f));
            var signAnimator = signRect.gameObject.AddComponent<Animator>();
            signAnimator.runtimeAnimatorController = Load<AnimatorController>(Animations + "JackpotSign.controller");
            sign = signRect.gameObject.AddComponent<JackpotSignView>();
            Set(sign, "animator", signAnimator);

            // Lever art (animated) plus an invisible button over the handle.
            var leverRect = Rect("Lever", machine, new Vector2(816, 624), Vector2.zero);
            AddImage(leverRect, LoadSprite(Machine + "lever_up.png"));
            var leverAnimator = leverRect.gameObject.AddComponent<Animator>();
            leverAnimator.runtimeAnimatorController = Load<AnimatorController>(Animations + "Lever.controller");
            var (handlePos, handleSize) = MachineRect(673, 296, 92, 270);
            var handleRect = Rect("LeverHandle", machine, handleSize, handlePos);
            var handleImage = AddImage(handleRect, white, new Color(1f, 1f, 1f, 0f), raycast: true);
            var handle = handleRect.gameObject.AddComponent<Button>();
            handle.targetGraphic = handleImage;
            handle.transition = Selectable.Transition.None;
            handle.navigation = new Navigation { mode = Navigation.Mode.None };
            lever = leverRect.gameObject.AddComponent<LeverView>();
            Set(lever, "animator", leverAnimator);
            Set(lever, "handle", handle);

            return machine;
        }

        private static HudView BuildHud(Transform stage, GameObject infoBoxPrefab, AudioService audioService,
            out PaytableView paytable, GameObject rowPrefab)
        {
            var hudRoot = Stretch("HUD", stage);
            var hud = hudRoot.gameObject.AddComponent<HudView>();

            // ----- left column: credits, bet, last win
            var credits = Instantiate(infoBoxPrefab, hudRoot, "CreditsBox", new Vector2(-477, 176));
            SetLabel(credits, "CREDITS");
            var bet = Instantiate(infoBoxPrefab, hudRoot, "BetBox", new Vector2(-477, 30));
            SetLabel(bet, "BET");
            var win = Instantiate(infoBoxPrefab, hudRoot, "WinBox", new Vector2(-477, -116));
            SetLabel(win, "LAST WIN");

            const string left = Buttons + "button_arrow_left_sheet.png";
            const string right = Buttons + "button_arrow_right_sheet.png";
            var betDown = AddSpriteButton(Rect("BetDown", bet.transform, new Vector2(40, 40), new Vector2(-96, -12)),
                LoadSprite(left, "arrow_left_normal"), LoadSprite(left, "arrow_left_highlight"),
                LoadSprite(left, "arrow_left_pressed"), LoadSprite(left, "arrow_left_disabled"));
            var betUp = AddSpriteButton(Rect("BetUp", bet.transform, new Vector2(40, 40), new Vector2(96, -12)),
                LoadSprite(right, "arrow_right_normal"), LoadSprite(right, "arrow_right_highlight"),
                LoadSprite(right, "arrow_right_pressed"), LoadSprite(right, "arrow_right_disabled"));
            AddClick(betDown, audioService);
            AddClick(betUp, audioService);

            var freeSpins = AddText("FreeSpins", hudRoot, body, "FREE SPINS: 3", 26, Mint, new Vector2(280, 24), new Vector2(-477, -196));

            // ----- right column: jackpot, paytable
            var jackpot = Instantiate(infoBoxPrefab, hudRoot, "JackpotBox", new Vector2(477, 196));
            SetLabel(jackpot, "JACKPOT");
            Value(jackpot).color = Gold;
            var jackpotHint = AddText("JackpotHint", hudRoot, body, "BET 100 TO WIN IT", 24, Lavender, new Vector2(280, 22), new Vector2(477, 124));

            // The panel art has ~26 px of transparent margin at this scale, so the
            // content is laid out inside a 248 x 292 visible area.
            var tableRoot = Rect("Paytable", hudRoot, new Vector2(300, 344), new Vector2(477, -92));
            AddImage(tableRoot, LoadSprite(Panels + "popup_panel.png"), type: Image.Type.Sliced, pixelsPerUnitMultiplier: 3f);
            AddText("Title", tableRoot, heading, "PAYTABLE", 14, Gold, new Vector2(240, 18), new Vector2(0, 130));
            var rows = new PaytableRow[4];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = Instantiate(rowPrefab, tableRoot, "Row_" + (i + 1), new Vector2(0, 84 - i * 52)).GetComponent<PaytableRow>();
            AddText("Rule", tableRoot, body, "ALL 3 REELS MUST MATCH", 22, Lavender, new Vector2(240, 20), new Vector2(0, -124));
            paytable = tableRoot.gameObject.AddComponent<PaytableView>();
            SetArray(paytable, "rows", rows);

            // ----- top banner and bottom controls hint
            var message = AddText("Message", hudRoot, heading, "PULL THE LEVER OR PRESS SPACE", 18, Lavender, new Vector2(760, 30), new Vector2(-43, 304));
            var controls = AddText("Controls", hudRoot, body, "SPACE: SPIN / STOP    ARROWS: BET\nM: SOUND    P: FILTER", 22,
                new Color(0.6f, 0.62f, 0.85f), new Vector2(290, 50), new Vector2(-477, -296));
            controls.lineSpacing = -8f;

            Set(hud, "creditsValue", Value(credits));
            Set(hud, "betValue", Value(bet));
            Set(hud, "winValue", Value(win));
            Set(hud, "jackpotValue", Value(jackpot));
            Set(hud, "jackpotHint", jackpotHint);
            Set(hud, "freeSpinsLabel", freeSpins);
            Set(hud, "message", message);
            Set(hud, "betDown", betDown);
            Set(hud, "betUp", betUp);
            return hud;
        }

        /// <summary>A square gold button with an on/off icon and a small caption underneath.</summary>
        private static IconToggleView BuildIconToggle(Transform parent, string name, string caption, Vector2 position,
            string onIcon, string offIcon, AudioService audioService)
        {
            const string sheet = Buttons + "button_gold_square_sheet.png";
            const string icons = Buttons + "icons_toggle_sheet.png";
            var rt = Rect(name, parent, new Vector2(44, 44), position);
            var button = AddSpriteButton(rt, LoadSprite(sheet, "gold_square_normal"), LoadSprite(sheet, "gold_square_highlight"),
                LoadSprite(sheet, "gold_square_pressed"), LoadSprite(sheet, "gold_square_disabled"));
            // The face sits a little above the button's dark bottom rim, so the icon does too.
            var icon = AddImage(Rect("Icon", rt, new Vector2(32, 32), new Vector2(0, 2)), LoadSprite(icons, onIcon));
            AddText("Caption", rt, body, caption, 18, Lavender, new Vector2(60, 16), new Vector2(0, -34));
            AddClick(button, audioService);

            var view = rt.gameObject.AddComponent<IconToggleView>();
            Set(view, "button", button);
            Set(view, "icon", icon);
            Set(view, "onSprite", LoadSprite(icons, onIcon));
            Set(view, "offSprite", LoadSprite(icons, offIcon));
            return view;
        }

        private static void SetLabel(GameObject infoBox, string text) =>
            infoBox.transform.Find("Label").GetComponent<TMP_Text>().text = text;

        private static TMP_Text Value(GameObject infoBox) => infoBox.transform.Find("Value").GetComponent<TMP_Text>();

        private static void AddClick(Button button, AudioService audioService)
        {
            var click = button.gameObject.AddComponent<ClickSound>();
            Set(click, "audioService", audioService);
        }
    }
}
