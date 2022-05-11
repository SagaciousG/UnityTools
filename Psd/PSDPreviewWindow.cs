using System;
using System.Collections.Generic;
using System.IO;
using Aspose.PSD.FileFormats.Psd;
using Aspose.PSD.FileFormats.Psd.Layers;
using Model;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UI;
using Image = Aspose.PSD.Image;

namespace XGame
{
    public class PSDPreviewWindow : EditorWindow
    {
        public static void ShowWin(PSDInfo psdInfo)
        {
            var win = GetWindow<PSDPreviewWindow>();
            win.Show();
            win.ReadFile(psdInfo);
        }

        private PSDInfo _psdInfo;
        private EditorTreeView<TreeLayer> _uiNewTree;
        private EditorTreeView<TreeLayer> _uiOldTree;

        private List<TreeLayer> _newTreeData;
        private List<TreeLayer> _oldTreeData;
        
        private TreeViewState _newtreeViewState;
        private TreeViewState _oldtreeViewState;

        private bool _changed;
        private bool _changed2;
        private int _controlId;
        private Vector2 _inspectorScroll;
        private TreeLayer _selectLayer;
        private GameObject _selectObj;
        private GameObject _prefabObj;
        private Vector2 _attrScroll;
        
        private HashSet<string> _uiNewUNames = new HashSet<string>();
        private HashSet<string> _uiOldUNames = new HashSet<string>();
        private List<Editor> _cachedEditors = new List<Editor>();
        
        private void OnEnable()
        {
            _newtreeViewState = new TreeViewState();
            _oldtreeViewState = new TreeViewState();
            
            _uiOldTree = new EditorTreeView<TreeLayer>(_oldtreeViewState);      
            _uiOldTree.OnDrawRowCallback += UiOldTreeOnOnDrawRowCallback;
            _uiOldTree.OnMoveElementsResult += UiOldTreeOnOnMoveElementsResult;
            _uiOldTree.BeforeDroppingDraggedItems += UiOldTreeOnBeforeDroppingDraggedItems;
            _uiOldTree.OnSingleClickedItem += UiOldTreeOnSingleClickedItem;
            _uiOldTree.OnDoubleClickItem += UiOldTreeOnOnDoubleClickItem;
            _uiOldTree.OnDropVerify += UiOldTreeOnOnDropVerify;
            _uiOldTree.OnContextClickedItem+= UiOldTreeOnOnContextClickedItem;
            
            _uiNewTree = new EditorTreeView<TreeLayer>(_newtreeViewState);
            _uiNewTree.OnDrawRowCallback += UiNewTreeOnOnDrawRowCallback;
            _uiNewTree.OnSingleClickedItem += UiNewTreeOnOnSingleClickedItem;
            _uiNewTree.OnDoubleClickItem += UiNewTreeOnOnSingleClickedItem;
            _uiNewTree.OnContextClickedItem += UiNewTreeOnOnContextClickedItem;
            _uiNewTree.BeforeDroppingDraggedItems += OnBeforeDroppingDraggedItems;
            _uiNewTree.OnDropVerify += UiNewTreeOnOnDropVerify;
            _uiNewTree.OnMoveElementsResult += UiNewTreeOnOnMoveElementsResult;
        }

        private void UiOldTreeOnOnContextClickedItem(int obj)
        {
            var layer = _uiOldTree.Data.Find((int) obj);
            var target = (GameObject) EditorUtility.InstanceIDToObject(layer.UID);
            if (target == null)
                return;
            var menu = new GenericMenu();
        }

        private void UiOldTreeOnOnDoubleClickItem(int obj)
        {
            var layer = _uiOldTree.Data.Find((int) obj);
            var target = (GameObject) EditorUtility.InstanceIDToObject(layer.UID);
            if (target == null)
                return;
            if (target.name == target.transform.root.name)
            {
                EditorGUIUtility.PingObject(target.transform.root.gameObject);
            }
            
            _uiNewTree.SetSelection(new List<int>(){obj});
            _uiNewTree.FrameItem(obj);
            _selectLayer = layer;
            _selectObj = (GameObject) EditorUtility.InstanceIDToObject(_selectLayer.UID);
            _cachedEditors.Clear();
            var coms = _selectObj.GetComponents<Component>();
            foreach (var com in coms)
            {
                _cachedEditors.Add(Editor.CreateEditor(com));
            }
        }

        private bool UiOldTreeOnOnDropVerify(TreeViewItem arg1, List<TreeViewItem> arg2)
        {
            return false;
        }

        private void UiOldTreeOnSingleClickedItem(int obj)
        {
            _uiNewTree.SetSelection(new List<int>(){obj});
            _uiNewTree.FrameItem(obj);
            var layer = _uiOldTree.Data.Find((int) obj);
            _selectLayer = layer;
            _selectObj = (GameObject) EditorUtility.InstanceIDToObject(_selectLayer.UID);
            _cachedEditors.Clear();
            var coms = _selectObj.GetComponents<Component>();
            foreach (var com in coms)
            {
                _cachedEditors.Add(Editor.CreateEditor(com));
            }
        }

        private void UiOldTreeOnBeforeDroppingDraggedItems(IList<TreeViewItem> obj)
        {
            foreach (TreeViewItem<TreeLayer> item in obj)
            {
                DepthRemove(item.Data, _uiOldUNames);
            }
            _uiNewTree.Reload();
            _uiOldTree.Reload();
        }

        private void UiNewTreeOnOnMoveElementsResult(TreeLayer parent, List<TreeViewItem> arg2)
        {
            foreach (TreeViewItem<TreeLayer> item in arg2)
            {
                DepthAdd(item.Data, _uiNewUNames);
            }
            _uiNewTree.Reload();
            _uiOldTree.Reload();
        }
        
        private void OnBeforeDroppingDraggedItems(IList<TreeViewItem> obj)
        {
            foreach (TreeViewItem<TreeLayer> item in obj)
            {
                DepthRemove(item.Data, _uiNewUNames);
            }
            _uiNewTree.Reload();
            _uiOldTree.Reload();
            _changed = true;
        }
        
        private void UiOldTreeOnOnMoveElementsResult(TreeLayer parent, List<TreeViewItem> arg2)
        {
            foreach (TreeViewItem<TreeLayer> treeViewItem in arg2)
            {
                DepthAdd(treeViewItem.Data, _uiOldUNames);
            }

            _changed2 = true;
            _uiNewTree.Reload();
            _uiOldTree.Reload();
        }

        private void DepthRemove(TreeLayer item, HashSet<string> names)
        {
            names.Remove(item.uName);
            if (item.HasChildren)
            {
                foreach (TreeLayer layer in item.Children)
                {
                    DepthRemove(layer, names);
                }
            }
        }
        
        private void DepthAdd(TreeLayer item, HashSet<string> names)
        {
            names.Add(item.uName);
            if (item.HasChildren)
            {
                foreach (TreeLayer layer in item.Children)
                {
                    DepthAdd(layer, names);
                }
            }
        }

        private bool UiNewTreeOnOnDropVerify(TreeViewItem arg1, List<TreeViewItem> arg2)
        {
            var parent = (TreeViewItem<TreeLayer>) arg1;
            if (parent == null)
                return false;
            if (parent.Data.LayerType == LayerType.Group)
                return true;
            return false;
        }
        
        private void DataOnModelChanged()
        {
            _changed = true;
            _uiNewTree.Reload();
        }



        private void UiNewTreeOnOnContextClickedItem(int id)
        {
            var layer = _uiNewTree.Data.Find(id);
            if (layer.PsdLayer?.IsRoot ?? false)
                return;
            var menu = new GenericMenu ();
            menu.AddItem(new GUIContent("删除层级"), false, RemoveLayer, new MenuItemArgs(id));
            if (layer.LayerType == LayerType.Group)
            {
                menu.AddItem (new GUIContent ("Add/Folder"), false, AddFolder, new MenuItemArgs(id));
            }
            menu.AddItem(new GUIContent("添加标签/引用(@ref)"), false, AddTag, 
                new MenuItemArgs(id)
                {
                    StrParam = "@ref"
                });
            menu.AddItem(new GUIContent("添加标签/忽略(@ignore)"), false, AddTag, 
                new MenuItemArgs(id)
                {
                    StrParam = "@ignore"
                });
            if (layer.LayerType == LayerType.Image)
            {
                menu.AddItem(new GUIContent("添加标签/空(@empty)"), false, AddTag,
                    new MenuItemArgs(id)
                    {
                        StrParam = "@empty"
                    });
            }
            menu.ShowAsContext ();
        }

        private void RemoveLayer(object obj)
        {
            var p = (MenuItemArgs) obj;
            var layer = _uiNewTree.Data.Find(p.Id);
            _uiNewTree.Data.RemoveElements(new List<int>(){p.Id});
            _uiNewTree.Reload();
        }
        
        
        private void AddFolder(object obj)
        {
            var p = (MenuItemArgs) obj;
            _changed = true;
            var layer = _uiNewTree.Data.Find(p.Id);
            var childrenCount = layer.Children?.Count ?? 0;
            var newLayer = new TreeLayer(){Name = $"NewLayer{childrenCount}",
                RealName = "NewLayer", LayerType = LayerType.Group};
            _uiNewTree.Data.AddElement(newLayer, layer, childrenCount);
            _uiNewTree.SetSelection(new List<int>(){newLayer.Id});
            _uiNewTree.FrameItem(newLayer.Id);
            _selectLayer = newLayer;
            _uiNewUNames.Add(newLayer.uName);
        }
                
        private void AddTag(object obj)
        {
            var p = (MenuItemArgs) obj;
            _changed = true;
            var layer = _uiNewTree.Data.Find(p.Id);
            if (!layer.RealName.Contains(p.StrParam))
            {
                layer.RealName = layer.RealName + p.StrParam;
                if (layer.PsdLayer != null)
                {
                    layer.PsdLayer.RealName = layer.RealName;
                    layer.PsdLayer.ReloadTags();
                }
            }
        }

        private void UiNewTreeOnOnSingleClickedItem(int obj)
        {
            var layer = _uiNewTree.Data.Find((int) obj);
            _selectLayer = layer;
            if (!_uiOldTree.Inited) return;
            _uiOldTree.SetSelection(new List<int>(){obj});
            _uiOldTree.FrameItem(obj);
        }

        private void UiOldTreeOnOnDrawRowCallback(TreeViewItemRow<TreeLayer> obj)
        {
            var indent = obj.GetContentIndent.Invoke(obj.item);
            var cellRect = obj.rowRect;
            cellRect.x += indent;

            if (!_uiNewUNames.Contains(obj.item.Data.uName))
            {
                obj.item.Data.Tag = UpdateTag.Remove;
            }
            else
            {
                obj.item.Data.Tag = UpdateTag.None;
            }
            
            var iconName = GetIcon(obj.item.Data.Tag);
            if (!string.IsNullOrEmpty(iconName))
            {
                var icon = EditorGUIUtility.IconContent(iconName);
                var iconRect = obj.rowRect;
                iconRect.y -= 5;
                EditorGUI.LabelField(iconRect, icon);
            }
            
            var flagRect = cellRect;
            flagRect.y -= 5;
            EditorGUI.LabelField(flagRect, EditorGUIUtility.IconContent(GetIcon(obj.item.Data.LayerType)));

            cellRect.x += 20;
            EditorGUI.LabelField(cellRect, $"<size=15>{obj.item.Data.Name}</size>", new GUIStyle(){richText = true});
            
        }

        private string GetIcon(UpdateTag tag)
        {
            switch (tag)
            {
                case UpdateTag.Remove:
                    return "d_winbtn_mac_close_h";
                case UpdateTag.Add:
                    return "d_winbtn_mac_max_h";
                case UpdateTag.Update:
                    return "d_winbtn_mac_min";
                case UpdateTag.Ignore:
                    return "winbtn_mac_min_h@2x";
            }

            return null;
        }

        private string GetIcon(LayerType type)
        {
            switch (type)
            {
                case LayerType.Group:
                    return "Folder Icon";
                case LayerType.Image:
                    return "RawImage Icon";
                case LayerType.Text:
                    return "Text Icon";
                case LayerType.GameObject:
                    return "PreMatCube@2x";
            }

            return null;
        }
        
        private void UiNewTreeOnOnDrawRowCallback(TreeViewItemRow<TreeLayer> obj)
        {
            var indent = obj.GetContentIndent.Invoke(obj.item);
            var cellRect = obj.rowRect;
            cellRect.x += indent;
            if (!_uiOldUNames.Contains(obj.item.Data.uName))
            {
                if (obj.item.Data.PsdLayer?.Ignore ?? false)
                    obj.item.Data.Tag = UpdateTag.Ignore;
                else
                    obj.item.Data.Tag = UpdateTag.Add;
            }
            else
            {
                obj.item.Data.Tag = UpdateTag.None;
            }
            
            var iconName = GetIcon(obj.item.Data.Tag);
            if (!string.IsNullOrEmpty(iconName))
            {
                var icon = EditorGUIUtility.IconContent(iconName);
                var rowRect = obj.rowRect;
                rowRect.y -= 5;
                EditorGUI.LabelField(rowRect, icon);
            }

            var iconRect = cellRect;
            iconRect.y -= 5;
            EditorGUI.LabelField(iconRect, EditorGUIUtility.IconContent(GetIcon(obj.item.Data.LayerType)));
            cellRect.x += 20;
            EditorGUI.LabelField(cellRect, $"<size=15>{obj.item.Data.Name}</size> <color=#807D7D><size=13>{obj.item.Data.RealName}</size></color>", 
                new GUIStyle(){richText = true});
 
        }

        private void ReadFile(PSDInfo psdInfo)
        {
            _psdInfo = psdInfo;
            var treeNew = new TreeLayer();
            _newTreeData = new List<TreeLayer>();
            _oldTreeData = new List<TreeLayer>();
            
            _newTreeData.Add(new TreeLayer(){Depth = -1, Name = ""});
            BuildUINewTree(treeNew, psdInfo.Root, 0);
            _uiNewTree.Build(new TreeModel<TreeLayer>(_newTreeData));
            _uiNewTree.Data.ModelChanged += DataOnModelChanged;
            
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(psdInfo.ParseInfo.TargetPath);
            if (prefab != null)
            {
                _prefabObj = prefab;
                var treeOld = new TreeLayer();
                _oldTreeData.Add(new TreeLayer(){Depth = -1, Name = ""});
                BuildUINewTree(treeOld, prefab.transform, 0);
                _uiOldTree.Build(new TreeModel<TreeLayer>(_oldTreeData));
                
                _uiOldTree.ExpandAll();
            }
            _uiNewTree.ExpandAll();
        }
        
        private void BuildUINewTree(TreeLayer tree, IPSDLayer layer, int depth)
        {
            tree.TargetType = TargetType.PSD;
            tree.Depth = depth;
            tree.RealName = layer.RealName;
            tree.Name = layer.Name;
            tree.UID = layer.UName.GetHashCode();
            tree.PsdLayer = layer;
            _uiNewUNames.Add(tree.uName);
            if (layer is PSDLayerGroup)
            {
                tree.LayerType = LayerType.Group;
            }else if (layer is PSDLayerText layerText)
            {
                tree.LayerType = LayerType.Text;
                tree.TextVal = layerText.Text;
            }
            else
            {
                tree.LayerType = LayerType.Image;
                var textures = AssetDatabase.FindAssets($"{((PSDLayerImage) layer).ImageName} t:Texture");
                foreach (var s in textures)
                {
                    var p = AssetDatabase.GUIDToAssetPath(s);
                    if (Path.GetFileNameWithoutExtension(p) == ((PSDLayerImage) layer).ImageName)
                    {
                        tree.Img = AssetDatabase.LoadAssetAtPath<Texture>(p);
                        break;
                    }
                }
            }

            _newTreeData.Add(tree);
            if (layer is PSDLayerGroup layerGroup)
            {
                tree.Children = new List<TreeElement>();
                for (int i = 0; i < layerGroup.PsdLayers.Count; i++)
                {
                    var item =  new TreeLayer();
                    item.Parent = tree;
                    tree.Children.Add(item);
                    BuildUINewTree(item, layerGroup.PsdLayers[i], depth + 1);
                }   
            }
        }
        
        private void BuildUINewTree(TreeLayer tree, Transform layer, int depth)
        {
            tree.TargetType = TargetType.Prefab;
            tree.Depth = depth;
            tree.Name = layer.name;
            tree.UID = layer.gameObject.GetInstanceID();
            if (layer.gameObject.GetComponent<Text>() != null)
                tree.LayerType = LayerType.Text;
            else if (layer.gameObject.GetComponent<Graphic>() != null)
                tree.LayerType = LayerType.Image;
            else
                tree.LayerType = LayerType.GameObject;
            _oldTreeData.Add(tree);
            _uiOldUNames.Add(tree.uName);
            if (layer.childCount > 0)
            {
                tree.Children = new List<TreeElement>();
                for (int i = 0; i < layer.childCount; i++)
                {
                    var item =  new TreeLayer();
                    item.Parent = tree;
                    tree.Children.Add(item);
                    BuildUINewTree(item, layer.GetChild(i), depth + 1);
                }   
            }
        }
        
        private void OnAttrWinGUI(Vector2 size)
        {
            if (_selectLayer == null)
                return;
            if (_selectLayer.TargetType == TargetType.PSD)
            {
                EditorGUILayout.LabelField("名称", _selectLayer.Name);
                EditorGUILayout.LabelField("PSD层级的名称");
                EditorGUI.BeginChangeCheck();
                _selectLayer.RealName = EditorGUILayout.DelayedTextField(_selectLayer.RealName, EditorStyles.textArea, GUILayout.Height(100));
                if (EditorGUI.EndChangeCheck())
                {
                    DepthRemove(_selectLayer, _uiNewUNames);
                    var ss = _selectLayer.RealName.Split('@');
                    if (ss.TryMatchOne(a => a.StartsWith("name="), out var str))
                    {
                        _selectLayer.Name = str.Replace("name=", "");
                    }
                    else
                    {
                        _selectLayer.Name = ss[0];
                    }
                    DepthAdd(_selectLayer, _uiNewUNames);
                    if (_selectLayer.PsdLayer != null)
                    {
                        _selectLayer.PsdLayer.RealName = _selectLayer.RealName;
                        _selectLayer.PsdLayer.ReloadTags();
                    }
                    _uiOldTree.Reload();
                    _uiNewTree.Reload();
                    _changed = true;
                }

                if (_selectLayer.PsdLayer?.Tags != null)
                {
                    foreach (var tag in _selectLayer.PsdLayer.Tags)
                    {
                        var color = _selectLayer.PsdLayer.ValidTag(tag) ? Color.white : Color.red;
                        var style = new GUIStyle("AppToolbarButtonLeft");
                        GUI.backgroundColor = color;
                        EditorGUILayout.LabelField(tag, style);
                        GUI.backgroundColor = Color.white;
                    }
                }
                
                EditorGUILayout.LabelField("层级类型", _selectLayer.LayerType.ToString());
                switch (_selectLayer.LayerType)
                {
                    case LayerType.Group:
                        break;
                    case LayerType.Image:
                        var width = position.width * 0.2f - 10;
                        if (_selectLayer.Img != null)
                        {
                            var rate = width / _selectLayer.Img.width;
                            var h = _selectLayer.Img.height * rate;
                            GUILayout.Box(_selectLayer.Img, GUILayout.Width(width), GUILayout.Height(h));
                        }
                        break;
                    case LayerType.Text:
                        EditorGUILayout.LabelField("Text", _selectLayer.TextVal);
                        break;
                }
            }
            else
            {
                _selectObj.name = EditorGUILayout.TextField("Name", _selectObj.name);
                _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
                foreach (var e in _cachedEditors)
                {
                    e.DrawHeader();
                    e.DrawDefaultInspector();
                }
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                
                EditorGUILayout.EndScrollView();
                if (GUI.changed)
                {
                    _changed2 = true;
                    EditorUtility.SetDirty(_prefabObj);
                    _controlId = GUIUtility.hotControl;
                }
            }

            if (EditorUtility.IsDirty(_prefabObj))
            {
                if (_controlId != GUIUtility.hotControl)
                    PrefabUtility.SavePrefabAsset(_prefabObj);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 35, position.width * 0.4f, 30));
            EditorGUILayout.BeginHorizontal();
            if (_changed)
            {
                if (GUILayout.Button("保存", GUILayout.Width(60)))
                {
                    Save();
                    var genInfo = GetWindow<PSD2UGUIWindow>().GetParseInfo(new FileInfo(_psdInfo.ParseInfo.Path));
                    ReadFile(genInfo);
                }

            }
            if (GUILayout.Button("删除错误标签", GUILayout.Width(100)))
            {
                RemoveInvalidTag();
                _changed = true;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
            
            GUILayout.BeginArea(new Rect(position.width *0.4f, 35, position.width * 0.4f, 30));
            if (_changed2)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("保存", GUILayout.Width(60)))
                {
                    SavePrefab();
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndArea();

            EditorGUI.LabelField(new Rect(position.width * 0.2f - 50, 0, 100, 30), "生成预览");
            EditorGUI.LabelField(new Rect(position.width * 0.6f - 50, 0, 100, 30), "现有预制体");
            
            
            _uiNewTree.OnGUI(new Rect(0, 70, position.width * 0.4f, position.height - 60));
            _uiOldTree.OnGUI(new Rect(position.width * 0.4f, 70, position.width *0.4f, position.height - 60));

            var attrRect = new Rect(position.width * 0.8f, 5, position.width * 0.2f, position.height);
            GUILayout.BeginArea(attrRect);
            _attrScroll = EditorGUILayout.BeginScrollView(_attrScroll);
            OnAttrWinGUI(attrRect.size);
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
            
        }

        private void RemoveInvalidTag()
        {
            var layers = _uiNewTree.Data.Data;
            for (var index = 2; index < layers.Length; index++)
            {
                var treeLayer = layers[index];
                if (treeLayer.PsdLayer?.Tags != null)
                {
                    foreach (var tag in treeLayer.PsdLayer.Tags)
                    {
                        if (!treeLayer.PsdLayer.ValidTag(tag))
                        {
                            treeLayer.PsdLayer.RealName = treeLayer.PsdLayer.RealName.Replace($"@{tag}", "");
                            treeLayer.RealName = treeLayer.PsdLayer.RealName;
                        }
                    }

                    treeLayer.PsdLayer.ReloadTags();
                }
            }
            _uiNewTree.Reload();
        }

        private void Save()
        {
            _changed = false;
            var layers = _uiNewTree.Data.Data;
            Aspose.Hook.Manager.StartHook();
            var psdImg = (PsdImage) Image.Load(_psdInfo.ParseInfo.Path);
            try
            {
                var psdLayers = new List<Layer>();
                var dividerIndex = new Stack<KeyValuePair<TreeLayer, int>>();
                for (var index = 1; index < layers.Length; index++)
                {
                    var treeLayer = layers[index];
                    var layer = _psdInfo.GetLayer(treeLayer.UID);
                    if (layer == null) //是一个新的Group
                    {
                        while (dividerIndex.Count > 0)
                        {
                            if (!treeLayer.Parent.Equals(dividerIndex.Peek().Key))
                            {
                                var ele = dividerIndex.Pop();
                                var group = psdImg.Layers[ele.Value];
                                group.DisplayName = $"Layer group: {ele.Key.RealName}";
                                psdLayers.Add(group);
                            }
                            else
                                break;
                        }

                        var idx = psdImg.Layers.Length;
                        psdImg.AddLayerGroup(treeLayer.Name, idx, false);
                        psdLayers.Add(psdImg.Layers[idx]);
                        if (treeLayer.HasChildren)
                        {
                            dividerIndex.Push(new KeyValuePair<TreeLayer, int>(treeLayer, idx + 1));
                        }
                        else
                        {
                            var group = (LayerGroup) psdImg.Layers[idx + 1];
                            group.DisplayName = $"Layer group: {treeLayer.RealName}";
                            psdLayers.Add(group);
                        }
                    }
                    else if (layer.IsRoot)
                    {

                    }
                    else
                    {
                        if (layer is PSDLayerGroup layerGroup) //
                        {
                            while (dividerIndex.Count > 0)
                            {
                                if (!treeLayer.Parent.Equals(dividerIndex.Peek().Key))
                                {
                                    var ele = dividerIndex.Pop();
                                    var group = psdImg.Layers[ele.Value];
                                    group.DisplayName = $"Layer group: {ele.Key.RealName}";
                                    psdLayers.Add(group);
                                }else
                                    break;
                            }
                            psdLayers.Add(psdImg.Layers[layerGroup.LayerDividerIndex]);

                            dividerIndex.Push(new KeyValuePair<TreeLayer, int>(treeLayer, layerGroup.LayerIndex));
                            if (!treeLayer.HasChildren)
                            {
                                var ele = dividerIndex.Pop();
                                var group = psdImg.Layers[ele.Value];
                                group.DisplayName = $"Layer group: {ele.Key.RealName}";
                                psdLayers.Add(group);
                            }
                        }
                        else
                        {
                            while (dividerIndex.Count > 0)
                            {
                                if (!treeLayer.Parent.Equals(dividerIndex.Peek().Key))
                                {
                                    var ele = dividerIndex.Pop();
                                    var group = psdImg.Layers[ele.Value];
                                    group.DisplayName = $"Layer group: {ele.Key.RealName}";
                                    psdLayers.Add(group);
                                }
                                else
                                    break;
                            }

                            var cur = psdImg.Layers[layer.LayerIndex];
                            cur.DisplayName = treeLayer.RealName;
                            
                            psdLayers.Add(cur);
                        }
                    }
                }

                while (dividerIndex.Count > 0)
                {
                    var ele = dividerIndex.Pop();
                    var group = psdImg.Layers[ele.Value];
                    group.DisplayName = $"Layer group: {ele.Key.RealName}";
                    psdLayers.Add(group);
                }

                var arr = psdLayers.ToArray();
                psdImg.Layers = arr;
                psdImg.Save();
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                psdImg.Dispose();
                
            }

        }

        

        private void SavePrefab()
        {
            _changed2 = false;
            var layers = _uiNewTree.Data.Data;
            for (int i = 1; i < layers.Length; i++)
            {
                var la = layers[i];
                if (la.Parent != null)
                {
                    var pObj = (GameObject) EditorUtility.InstanceIDToObject(((TreeLayer) la.Parent).UID);
                    if (pObj != null)
                    {
                        var layerObj = (GameObject) EditorUtility.InstanceIDToObject(la.UID);
                        if (layerObj != null)
                        {
                            layerObj.transform.SetParent(pObj.transform, true);
                        }
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }
        private void OnDestroy()
        {
            if (_changed || _changed2)
            {
                var tip = EditorUtility.DisplayDialog("提示", "更改尚未保存，是否保存？", "保存", "关闭");
                if (tip)
                {
                    Save();
                }
            }
        }

        private class TreeLayer : TreeElement
        {
            public override int Id
            {
                get
                {
                    if (uName == null)
                    {
                        return 0;
                    }
                    return uName.GetHashCode();
                }
            }

            public string RealName;

            public string uName
            {
                get
                {
                    var p = (TreeLayer) Parent;
                    return $"{p?.uName}-{Name}".Trim('-');
                }
            }

            public TargetType TargetType;
            public UpdateTag Tag;
            public LayerType LayerType;
            public int UID;
            public IPSDLayer PsdLayer;
            public Texture Img;
            public string TextVal;
        }

        private enum UpdateTag
        {
            None,
            Add,
            Update,
            Remove,
            Ignore,
        }
        
        private enum LayerType
        {
            Group,
            Image,
            Text,
            GameObject
        }
        
        private enum TargetType
        {
            PSD,
            Prefab
        }

        private class MenuItemArgs
        {
            public MenuItemArgs(int id)
            {
                Id = id;
            }
            public int Id;
            public string StrParam;
        }

    }
}