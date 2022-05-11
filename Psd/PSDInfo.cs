﻿using System;
using System.Collections.Generic;
using System.IO;
 using System.Linq;
 using Aspose.PSD;
using Aspose.PSD.FileFormats.Psd;
using Aspose.PSD.FileFormats.Psd.Layers;
using Aspose.PSD.ImageOptions;
 using Model;
 using UnityEngine;

namespace XGame
{
    public class PSDInfo
    {
        public PSDLayerGroup Root;
        public PSDParseInfo ParseInfo;

        private HashSet<string> _layerNames;
        private string _path;
        private Dictionary<int, IPSDLayer> _layersMap;
        public PSDInfo(string path, PSDParseInfo parseInfo)
        {
            ParseInfo = parseInfo;
            _path = path;
            _layerNames = new HashSet<string>();
            _layersMap = new Dictionary<int, IPSDLayer>();
            var psdImage = (PsdImage) Image.Load(path);
            Root = new PSDLayerGroup();
            Root.Size = new Vector2(psdImage.Width, psdImage.Height);
            var fileName = Path.GetFileNameWithoutExtension(path);
            Root.RealName = fileName;
            Root.Name = fileName.Substring(fileName.IndexOf('@') + 1);
            Root.UName = Root.Name;
            Root.CenterPosition = Vector2.zero;
            Root.IsRoot = true;
            _layersMap[Root.UName.GetHashCode()] = Root;
            var stack = new Stack<PSDLayerGroup>();
            stack.Push(Root);
            try
            {
                for (var index = 0; index < psdImage.Layers.Length; index++)
                {
                    var layer = psdImage.Layers[index];
                    InLayerGroup(layer, index, stack);
                }
            }
            finally
            {
                psdImage.Dispose();
            }
            FinalVerify(Root);
        }

        public IPSDLayer GetLayer(int uid)
        {
            _layersMap.TryGetValue(uid, out var layer);
            return layer;
        }
        
        public void SaveImage(string dirPath)
        {
            var pngOption = new PngOptions();
            var psdImage = (PsdImage) Image.Load(_path);
            foreach (var layer in psdImage.Layers)
            {
                if (!(layer is SectionDividerLayer || layer is TextLayer))
                {
                    if (layer is LayerGroup)
                    {
                        if (layer.Name.Contains("@img="))
                        {
                            var idx = layer.Name.IndexOf("@img=", StringComparison.Ordinal);
                            var imageName = layer.Name.Substring(0, idx);
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            layer.Save($"{dirPath}/{imageName}.png", pngOption);
                        }
                    }
                    else
                    {
                        var imageName = layer.Name;
                        if (layer.Name.Contains("@name="))
                        {
                            var idx = layer.Name.IndexOf("@name=", StringComparison.Ordinal);
                            imageName = layer.Name.Substring(0, idx);
                        }
            
                        if (!Directory.Exists(dirPath))
                            Directory.CreateDirectory(dirPath);
                        layer.Save($"{dirPath}/{imageName}.png",
                            pngOption);
                    }
                  
                }
            }
        }
        
        private void InLayerGroup(Layer l, int index, Stack<PSDLayerGroup> stack)
        {
            if (l is SectionDividerLayer || l.DisplayName == "</Layer group>")
            {
                var psdGroup = new PSDLayerGroup();
                psdGroup.Parent = stack.Peek();
                psdGroup.Parent.PsdLayers.Add(psdGroup);
                psdGroup.LayerDividerIndex = index;
                stack.Push(psdGroup);
                return;
            }

           
            IPSDLayer psdLayer = null;
            var group = stack.Peek();
            switch (l)
            {
                case LayerGroup p1:
                    psdLayer = group;
                    break;
                case TextLayer p2:
                    psdLayer = new PSDLayerText();
                    psdLayer.Parent = @group;
                    break;
                default:
                    psdLayer = new PSDLayerImage();
                    psdLayer.Parent = @group;
                    break;
            }
            

            psdLayer.RealName = l.DisplayName.Replace("Layer group: ", "");
            var nameMatches = psdLayer.RealName.Split('@');
            psdLayer.Name = nameMatches.TryMatchOne(a => a.StartsWith("name="), out var res)
                ? res.Replace("name=", "")
                : nameMatches[0];
            psdLayer.LayerIndex = index;
            psdLayer.Tags = new string[nameMatches.Length - 1];
            if (psdLayer.Tags.Length > 0)
                Array.Copy(nameMatches, 1, psdLayer.Tags, 0, psdLayer.Tags.Length);
            
            if (l is LayerGroup layerGroup)
            {
                (group.CenterPosition, group.Size) = GetPosSize(layerGroup);
                stack.Pop();
                return;
            } 
           
            if (l is TextLayer textLayer)
            {
                var text = psdLayer as PSDLayerText;
                var font = textLayer.Font;
                text.Text = textLayer.InnerText;
                text.FontSize = font.Size;
                text.Bold = font.Bold;
                text.Italic = font.Italic;
                text.Color = new UnityEngine.Color(textLayer.TextColor.R / 255f, textLayer.TextColor.G / 255f, textLayer.TextColor.B / 255f, textLayer.TextColor.A);
            }
            else
            {
                var image = psdLayer as PSDLayerImage;
                image.ImageLayer = l;
                image.Alpha = l.FillOpacity;
                image.ImageName = nameMatches[0];
            }
            
            psdLayer.Size = new Vector2(l.Width, l.Height);
            psdLayer.CenterPosition = GetAnchorPos(l);
            psdLayer.Visible = l.IsVisible;
            @group.PsdLayers.Add(psdLayer);
        }

        private void FindBound(LayerGroup layerGroup, ref int l, ref int r, ref int t, ref int b)
        {
            foreach (var g in layerGroup.Layers)
            {
                if (g is LayerGroup lg)
                {
                    FindBound(lg, ref l, ref r, ref t, ref b);
                }
                else if (! (g is SectionDividerLayer))
                {
                    l = Mathf.Min(l, g.Left);
                    r = Mathf.Max(r, g.Right);
                    t = Mathf.Min(t, g.Top);
                    b = Mathf.Max(b, g.Bottom);
                }
            }
        }
        
        private Vector2 GetAnchorPos(Layer layer)
        {
            var t = layer.Top;
            var l = layer.Left;
            var r = layer.Right;
            var b = layer.Bottom;
            var center = new Vector2(Root.Size.x / 2, Root.Size.y / 2);
            var pos = new Vector2((l + r) / 2f, (t + b) / 2f);
            return new Vector2(pos.x - center.x, center.y - pos.y);
        }
        
        
        private (Vector2, Vector2) GetPosSize(LayerGroup layerGroup)
        {
            var l = 9999;
            var r = 0;
            var t = 9999;
            var b = 0;
            FindBound(layerGroup, ref l, ref r, ref t, ref b);
            var center = new Vector2(Root.Size.x / 2, Root.Size.y / 2);
            var pos = new Vector2((l + r) / 2f, (t + b) / 2f);
            return (new Vector2(pos.x - center.x, center.y - pos.y),
                new Vector2(r - l, b - t));
        }
        
        private void VerifyNames(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;
            if (!_layerNames.Contains(name))
            {
                _layerNames.Add(name);
            }
            else
            {
                throw new Exception($"{Root.Name}存在重复名称{name}, 可能会导致重新生成时数据错乱或丢失");
            }
            
        }
        
        
        private void FinalVerify(IPSDLayer group)
        {
            if (group.Parent != null)
            {
                group.UName = $"{group.Parent.UName}-{group.Name}";
                _layersMap[group.UName.GetHashCode()] = group;
                if (!group.Ignore)
                {
                    VerifyNames(group.UName);
                }
            }
            if (group is PSDLayerGroup layerGroup)
            {
                foreach (var psdLayer in layerGroup.PsdLayers)
                {
                    FinalVerify(psdLayer);
                }
            }
        }
        
    }
}