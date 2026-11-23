using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SlotMachine.EditorTools
{
    /// <summary>
    /// Small helpers shared by the project builders: creating UI objects,
    /// loading sliced sprites and assigning private serialized fields.
    /// </summary>
    internal static class BuilderUtility
    {
        public const int UILayer = 5;

        // ------------------------------------------------------------ assets

        public static Sprite LoadSprite(string path, string name = null)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            var sprite = name == null ? sprites.FirstOrDefault() : sprites.FirstOrDefault(s => s.name == name);
            if (sprite == null)
                throw new InvalidOperationException("Sprite '" + (name ?? "(single)") + "' not found in " + path);
            return sprite;
        }

        public static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException(typeof(T).Name + " not found at " + path);
            return asset;
        }

        public static void DeleteIfExists(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
        }

        // ------------------------------------------------- serialized fields

        public static void Set(Object target, string field, Object value) =>
            Edit(target, field, p => p.objectReferenceValue = value);

        public static void Set(Object target, string field, int value) =>
            Edit(target, field, p => p.intValue = value);

        public static void Set(Object target, string field, float value) =>
            Edit(target, field, p => p.floatValue = value);

        public static void Set(Object target, string field, string value) =>
            Edit(target, field, p => p.stringValue = value);

        public static void SetEnum(Object target, string field, int index) =>
            Edit(target, field, p => p.enumValueIndex = index);

        public static void SetArray(Object target, string field, Object[] values) =>
            Edit(target, field, p =>
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                    p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            });

        public static void Edit(Object target, string field, Action<SerializedProperty> apply)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException(target.GetType().Name + " has no serialized field '" + field + "'");
            apply(property);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------- UI objects

        /// <summary>A RectTransform child anchored at the parent's centre (unless told otherwise).</summary>
        public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position,
            Vector2? anchor = null, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UILayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var a = anchor ?? new Vector2(0.5f, 0.5f);
            rt.anchorMin = rt.anchorMax = a;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return rt;
        }

        /// <summary>A RectTransform child that fills its parent.</summary>
        public static RectTransform Stretch(string name, Transform parent)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image AddImage(RectTransform rt, Sprite sprite, Color? color = null, bool raycast = false,
            Image.Type type = Image.Type.Simple, float pixelsPerUnitMultiplier = 1f)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color ?? Color.white;
            image.raycastTarget = raycast;
            image.type = type;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            return image;
        }

        public static TextMeshProUGUI AddText(string name, Transform parent, TMP_FontAsset font, string text,
            float size, Color color, Vector2 rectSize, Vector2 position,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var rt = Rect(name, parent, rectSize, position);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>A button that swaps between the four states of a sliced sprite sheet.</summary>
        public static Button AddSpriteButton(RectTransform rt, Sprite normal, Sprite highlighted, Sprite pressed, Sprite disabled)
        {
            var image = AddImage(rt, normal, raycast: true);
            image.preserveAspect = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = highlighted,
                pressedSprite = pressed,
                selectedSprite = normal, // don't stay lit after a click
                disabledSprite = disabled
            };
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        /// <summary>Converts a rectangle measured on the 816x624 machine art (top-left origin) to centred local space.</summary>
        public static (Vector2 position, Vector2 size) MachineRect(float x, float y, float width, float height) =>
            (new Vector2(x + width / 2f - 408f, 312f - (y + height / 2f)), new Vector2(width, height));
    }
}
