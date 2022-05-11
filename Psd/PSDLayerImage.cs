﻿using System.IO;
using System.Linq;
using Aspose.PSD.FileFormats.Psd.Layers;
 using Model;
 using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace XGame
{
    public class PSDLayerImage : IPSDLayer
    {
        public int Alpha;
        public string ImageName;
        public Layer ImageLayer;
        //仅用于创建RectTransform，不带Image组件
        public bool Empty => Tags?.Contains("empty") ?? false;

        public override void SetVariableValue(RectTransform rect)
        {
            foreach (var tag in Tags)
            {
                if (tag.StartsWith("name="))
                {
                    Name = tag.Replace("name=", "").Trim();
                    rect.gameObject.name = Name;
                }else if (tag.StartsWith("ys="))
                {
                    var prefab = tag.Replace("ys=", "").Trim();
                    if (!string.IsNullOrEmpty(prefab))
                    {
                        var sub = rect.GetComponent<SubUIAgent>();
                        if (sub == null)
                            sub = rect.gameObject.AddComponent<SubUIAgent>();
                        sub.PrefabAsset = prefab;
                    }
                }
            }
      
            if (!Empty)
            {
                var image = rect.GetComponent<XImage>();
                if (image == null)
                {
                    Debug.LogError($"{rect.gameObject.name}不存在组件Image，请检查是否需要被标记为Empty");
                    return;
                }
                var c = image.color;
                c.a = Alpha / 100f;
                image.color = c;
                var sprites = AssetDatabase.FindAssets($"t:Sprite {ImageName}");
                foreach (var s in sprites)
                {
                    var p = AssetDatabase.GUIDToAssetPath(s);
                    if (Path.GetFileNameWithoutExtension(p) == ImageName)
                    {
                        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                        if (image.sprite.border != Vector4.zero)
                        {
                            image.type = Image.Type.Sliced;
                        }
                        break;
                    }
                }
                
            }

     
        }

        public override void SetDefaultValue(RectTransform obj)
        {
            var img = obj.GetComponent<XImage>();
            if (img == null && !Empty)
                img = obj.gameObject.AddComponent<XImage>();
        }
        
        public override bool ValidTag(string tag)
        {
            if (tag.StartsWith("ys=") || tag == "empty" || tag.StartsWith("name="))
                return true;
            return base.ValidTag(tag);
        }
    }
}