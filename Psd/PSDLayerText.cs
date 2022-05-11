using System;
using System.IO;
using Model;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace XGame
{
    public class PSDLayerText : IPSDLayer
    {
        public string Text;
        public float FontSize;
        public Color Color;
        public string Font;
        public bool Bold;
        public bool Italic;

        public override void SetVariableValue(RectTransform rect)
        {
            foreach (var tag in Tags)
            {
                if (tag.Contains("font="))
                {
                    Font = tag.Replace("font=", "").Trim();
                }
                else if (tag.Contains("size="))
                {
                    FontSize = Convert.ToInt32(tag.Replace("size=", ""));
                }
               
            }
            
            var text = rect.GetComponent<XText>();
            text.color = Color;
            text.fontSize = (int) FontSize;
            text.text = Text;
            if (!string.IsNullOrEmpty(Font))
            {
                var fonts = AssetDatabase.FindAssets($"t:Font {Font}");
                foreach (var f in fonts)
                {
                    var p = AssetDatabase.GUIDToAssetPath(f);
                    if (Path.GetFileNameWithoutExtension(p) == Font)
                    {
                        text.font =  AssetDatabase.LoadAssetAtPath<Font>(p);
                        break;
                    }
                }
            }
            else
            {
                var setting = AssetDatabase.LoadAssetAtPath<PSDSetting>(PathUtil.PSDSetting);
                text.font = setting.DefaultFont;
            }
            
            if (Bold && Italic)
            {
                text.fontStyle = FontStyle.BoldAndItalic;
            }
            else if (Bold)
            {
                text.fontStyle = FontStyle.Bold;
            }else if (Italic)
            {
                text.fontStyle = FontStyle.Italic;
            }
            else
            {
                text.fontStyle = FontStyle.Normal;
            }
        }

        public override void SetDefaultValue(RectTransform obj)
        {
            var t = obj.GetComponent<XText>();
            if (t == null)
                t = obj.gameObject.AddComponent<XText>();
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
        }
        
        public override bool ValidTag(string tag)
        {
            if (tag.StartsWith("font=") || tag.StartsWith("size="))
                return true;
            return base.ValidTag(tag);
        }
    }
}