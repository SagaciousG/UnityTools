﻿using System.Collections.Generic;
using System.IO;
 using System.Linq;
 using Model;
 using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace XGame
{
    public struct PSDParseInfo
    {
        public string Path;
        //导出的Prefab名
        public string Name;
        public string PublicPath;
        public string ToFolder;
        public string ImageSavePath;
        public bool ExportImage;
        public PSDSetting Setting;
        
        public string TargetPath =>  $"{ToFolder}/{PublicPath}/{Name}.prefab";
    }
    
    public static class PSDParse
    {
        private static PSDParseInfo _info;
        private static Vector2 _screenSize;
        public static void Parse(PSDParseInfo info, Vector2 screenSize)
        {
            _info = info;
            _screenSize = screenSize;
            var file = new PSDInfo(info.Path, info);
            if (info.ExportImage)
            {
                file.SaveImage(info.ImageSavePath);
                AssetDatabase.Refresh();
            }
            
            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                EditorApplication.ExecuteMenuItem("GameObject/UI/Canvas");
                canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            }
           
            var target = info.TargetPath;
            if (!Directory.Exists(Path.GetDirectoryName(target)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target));
            }
            if (File.Exists(target))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(target);
                var obj = (GameObject) PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
                PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                var objs = new List<Object>();
                UpdatePrefab(file.Root, obj, objs);
                PrefabUtility.SaveAsPrefabAssetAndConnect(obj.gameObject, target, InteractionMode.AutomatedAction);
                EditorGUIUtility.PingObject(obj.gameObject);
            }
            else
            {
                RectTransform obj = new GameObject(file.Root.Name).AddComponent<RectTransform>();
                obj.SetParent(canvas.transform, false);
                file.Root.SetTransform(obj, screenSize);
                var objs = new List<Object>();
                Group2UGUI(obj, file.Root, objs);
                if (!Directory.Exists(Path.GetDirectoryName(target)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                }
                EditorGUIUtility.PingObject(obj.gameObject);
                PrefabUtility.SaveAsPrefabAssetAndConnect(obj.gameObject, target, InteractionMode.AutomatedAction);
            }
        }

        private static void UpdatePrefab(PSDLayerGroup layer, GameObject node, List<Object> objs)
        {
            var nodeRect = node.GetComponent<RectTransform>();
            var allChild = node.transform.Children().ToList();
            layer.SetTransform(nodeRect, _screenSize);
            layer.SetVariableValue(nodeRect);
            var index = 0;
            foreach (var psdLayer in layer.PsdLayers)
            {
                if (psdLayer.Ignore)
                    continue;
                var nodeTrans = node.transform.Find(psdLayer.Name);
                if (nodeTrans == null)
                {
                    nodeTrans = new GameObject(psdLayer.Name).AddComponent<RectTransform>();
                    nodeTrans.SetParent(nodeRect);
                    psdLayer.SetDefaultValue((RectTransform) nodeTrans);
                }
                else
                {
                    allChild.Remove(nodeTrans);
                }
                
                if (psdLayer is PSDLayerGroup group)
                {
                    UpdatePrefab(group, nodeTrans.gameObject, objs);
                }
                else
                {
                    var rect = nodeTrans.GetComponent<RectTransform>();
                    psdLayer.SetTransform(rect, _screenSize);
                    psdLayer.SetVariableValue(rect);
                }
                nodeTrans.SetSiblingIndex(index);
                index++;

                if (psdLayer.Reference)
                {
                    objs.Add(nodeTrans.gameObject);
                }
            }

            foreach (var transform in allChild)
            {
                Object.DestroyImmediate(transform.gameObject);
            }
        }
        
        private static void Group2UGUI(RectTransform node, PSDLayerGroup group, List<Object> objs)
        {
            foreach (var psdLayer in group.PsdLayers)
            {
                if (psdLayer.Ignore)
                    continue;
                RectTransform lo = null;
                lo = CreateRect(psdLayer);
                if (lo != null)
                {
                    lo.transform.SetParent(node.transform, false);
                    psdLayer.SetTransform(lo, _screenSize);
                }
                if (psdLayer is PSDLayerGroup g)
                {
                    Group2UGUI(lo, g, objs);
                    g.SetVariableValue(lo);
                }else if (psdLayer is PSDLayerImage image)
                {
                    image.SetDefaultValue(lo);
                    image.SetVariableValue(lo);
                }else if (psdLayer is PSDLayerText text)
                {
                    text.SetDefaultValue(lo);
                    text.SetVariableValue(lo);
                }

                if (psdLayer.Reference)
                {
                    objs.Add(lo.gameObject);
                }
            }
        }

        private static void CopyGameObject(GameObject from, GameObject to)
        {
            var components = from.GetComponents<Component>();
            foreach (var c in components)
            {
                ComponentUtility.CopyComponent(c);
                if (to.TryGetComponent(c.GetType(), out var target))
                {
                    ComponentUtility.PasteComponentValues(target);
                }
                else
                {
                    ComponentUtility.PasteComponentAsNew(to);
                }
            }
        }
        
        private static RectTransform CreateRect(IPSDLayer layer)
        {
            var obj = new GameObject(layer.Name);
            var rect = obj.AddComponent<RectTransform>();
            return rect;
        }
    }
}