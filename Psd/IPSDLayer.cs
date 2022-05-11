using System;
using System.Linq;
using UnityEngine;

namespace XGame
{
    public abstract class IPSDLayer
    {
        public PSDLayerGroup Parent { set; get; }
        public string RealName { get; set; }
        public string Name { get; set; }
        public string UName { get; set; }
        public Vector2 Size { set; get; }
        public Vector2 CenterPosition { set; get; }
        public bool Ignore
        {
            get
            {
                if (IsRoot)
                    return false;
                var res = Tags.Contains("ignore") || (Parent?.Ignore ?? false);
                return res;
            }
        }

        public bool Visible = true;
        public bool Reference => Tags.Contains("ref");
        public bool IsRoot;
        public int LayerIndex;
        public string[] Tags;
        
        public void SetTransform(RectTransform transform, Vector2 screenSize)
        {
            transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Size.x);
            transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Size.y);
            transform.position = CenterPosition + new Vector2(screenSize.x / 2, screenSize.y / 2);
            transform.gameObject.SetActive(Visible);
        }
        
        public abstract void SetVariableValue(RectTransform rect);
        public abstract void SetDefaultValue(RectTransform obj);

        public virtual bool ValidTag(string tag)
        {
            if (tag == "ignore" || tag == "ref")
                return true;
            return false;
        }

        public void ReloadTags()
        {
            var nameMatches = RealName.Split('@');
            Tags = new string[nameMatches.Length - 1];
            if (Tags.Length > 0)
                Array.Copy(nameMatches, 1, Tags, 0, Tags.Length);
        }
    }
}