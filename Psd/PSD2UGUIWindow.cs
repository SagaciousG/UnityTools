using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Model;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XGame
{
    public class PSD2UGUIWindow : SearchableEditorWindow
    {
        [MenuItem("UGUI/PSD2UGUI")]
        static void ShowWin()
        {
            var win = GetWindow<PSD2UGUIWindow>();
            win.Show();
        }

        private string _dirPath = "./Psd";
        private string _toFolder = "Assets/UI";
        private string _imgPath = "Assets/Images/UI";
        private string _settingAssets => PathUtil.PSDSetting;
        private AutocompleteSearchField _searchField;
        private Vector2 _scrollPos;
        private Vector2 _rightScrollPos;
        private Dictionary<string, bool> _folderShow = new Dictionary<string, bool>();
        private List<string> _latestOpened = new List<string>();
        private string _selectedDir;
        private PSDSetting _setting;
        private ReorderableList _componentList;
        private SerializedObject _settingObj;
        private string _operatingFile;
        
        public override void OnEnable()
        {
            _searchField = new AutocompleteSearchField();
            if (File.Exists(_settingAssets.Replace("Assets", Application.dataPath)))
            {
                _setting = AssetDatabase.LoadAssetAtPath<PSDSetting>(_settingAssets);
            }
            else
            {
                _setting = CreateInstance<PSDSetting>();
                AssetDatabase.CreateAsset(_setting, _settingAssets);
            }

            _dirPath = _setting.PsdFolder;
            _toFolder = _setting.UIFolder;
            _latestOpened = _setting.LatesdOpened;
            _settingObj = new SerializedObject(_setting);
            var prop = _settingObj.FindProperty("ComponentTypes");
            _componentList = new ReorderableList(_settingObj, prop, true, true, true, true);
            _componentList.drawElementCallback += (rect, index, active, focused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.y += 2;
                EditorGUI.PropertyField(rect,prop.GetArrayElementAtIndex(index), new GUIContent($"{index}"));
            };
            _componentList.drawHeaderCallback += rect =>
            {
                GUI.Label(rect, "组件优先级");
            };
        }

        private void OnGUI()
        {
            var top = new Rect(0, 0, position.width - 305, 100);
            var right = new Rect(position.width - 300, 0, 300, position.height);
            var center = new Rect(0, top.height + 5, position.width - 305, position.height - top.height - 10);
            GUILayout.BeginArea(top, "", "box");
            OnTopGUI();
            GUILayout.EndArea();
            
            GUILayout.BeginArea(center, "");
            OnCenterGUI();
            GUILayout.EndArea();

            GUILayout.BeginArea(right, "", "box");
            OnRightGUI();
            GUILayout.EndArea();
        }

        private void OnTopGUI()
        {
            EditorGUILayout.BeginHorizontal();
            _dirPath = EditorGUILayout.TextField("文件夹路径", _dirPath, GUILayout.Width(position.width / 2));
            if (GUILayout.Button("打开文件夹", GUILayout.Width(100)))
            {
                var folderInfo = new DirectoryInfo(_dirPath);
                System.Diagnostics.Process.Start("explorer.exe", folderInfo.FullName);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            _toFolder = EditorGUILayout.TextField("目标文件夹", _toFolder, GUILayout.Width(position.width / 2));
            if (GUILayout.Button("打开文件夹", GUILayout.Width(100)))
            {
                var folderInfo = new DirectoryInfo(_toFolder);
                System.Diagnostics.Process.Start("explorer.exe", folderInfo.FullName);
            }
            EditorGUILayout.EndHorizontal();
            _searchField.OnGUI();

            if (_latestOpened.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("最近打开", GUILayout.Width(100));
                for (int i = 0; i < _latestOpened.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(200));
                    var dir = new DirectoryInfo(_latestOpened[i]);
                    if (!dir.Exists)
                    {
                        _latestOpened.RemoveAt(i);
                        return;
                    }

                    var btnName = dir.FullName.Replace("\\", "/")
                        .Replace(_dirPath.Replace("\\", "/"), "");
                    GUI.backgroundColor = _selectedDir == dir.FullName ? Color.green : Color.white;
                    if (GUILayout.Button(btnName))
                    {
                        if (_selectedDir == dir.FullName)
                            _selectedDir = "";
                        else
                            _selectedDir = dir.FullName;
                    }
                    GUI.backgroundColor = Color.white;

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        if (_selectedDir == _latestOpened[i])
                            _selectedDir = "";
                        _latestOpened.RemoveAt(i);
                        return;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorUtility.SetDirty(_setting);
        }

        private void OnCenterGUI()
        {
            if (!Directory.Exists(_dirPath))
                return;
            _setting.PsdFolder = _dirPath;
            _setting.UIFolder = _toFolder;
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, "box");
            var dir = string.IsNullOrEmpty(_selectedDir) ? _dirPath : _selectedDir;            
            var folderInfo = new DirectoryInfo(dir);
            if (string.IsNullOrEmpty(_searchField.searchString))
            {
                var files = folderInfo.GetFiles("*.psd");
                if (files.Length > 0)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        GUI.backgroundColor = i % 2 == 0 ? new Color(220 / 255f, 220 / 255f, 220 / 255f) : Color.white;
                        DrawFile(files[i], 0);   
                    }
                    GUI.backgroundColor = Color.white;
                }
                foreach (var d in folderInfo.GetDirectories())
                {
                    DrawFolder(d, 0);
                }
            }
            else
            {
                var files = folderInfo.GetFiles("*.psd", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (files[i].Name.Contains(_searchField.searchString))
                        {
                            GUI.backgroundColor = i % 2 == 0 ? new Color(220 / 255f, 220 / 255f, 220 / 255f) : Color.white;
                            DrawFile(files[i], 0);   
                        }   
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawFolder(DirectoryInfo dir, int tuck)
        {
            _folderShow.TryGetValue(dir.FullName, out var flag);
            EditorGUILayout.BeginHorizontal("dockareaStandalone", GUILayout.Height(40));
            EditorGUILayout.LabelField("", GUILayout.Width(20 * tuck));
            flag = EditorGUILayout.Foldout(flag, dir.Name);
            if (GUILayout.Button("批量生成", GUILayout.Width(100)))
            {
                if (!OpenNewScene())
                    return;
                PushInLatestOpened(dir.FullName);
                foreach (var fileInfo in dir.GetFiles("*.psd", SearchOption.AllDirectories))
                {
                    GenFile(fileInfo, false);
                }
            }
            // if (GUILayout.Button("批量生成并导出图片", GUILayout.Width(200)))
            // {
            //     foreach (var fileInfo in dir.GetFiles("*.psd", SearchOption.AllDirectories))
            //     {
            //         GenFile(fileInfo, true);
            //     }
            // }
            EditorGUILayout.EndHorizontal();
            
            
            _folderShow[dir.FullName] = flag;
            if (flag)
            {
                var files = dir.GetFiles("*.psd");
                if (files.Length > 0)
                {
                    EditorGUILayout.BeginVertical("box");
                    for (int i = 0; i < files.Length; i++)
                    {
                        GUI.backgroundColor = i % 2 == 0 ? new Color(220 / 255f, 220 / 255f, 220 / 255f) : Color.white;
                        DrawFile(files[i], tuck + 1);   
                    }
                    EditorGUILayout.EndVertical();
                }
                GUI.backgroundColor = Color.white;
                foreach (var d in dir.GetDirectories())
                {
                    DrawFolder(d, tuck + 1);
                }
            }
        }

        private void OnRightGUI()
        {
            EditorGUILayout.BeginVertical();
            _setting.DefaultFont = (Font) EditorGUILayout.ObjectField("默认字体", _setting.DefaultFont, typeof(Font), false);
            _setting.ScreenSize = EditorGUILayout.Vector2Field("屏幕尺寸", _setting.ScreenSize);
            EditorGUILayout.EndVertical();
            
            _rightScrollPos = EditorGUILayout.BeginScrollView(_rightScrollPos, "box", GUILayout.MaxHeight(400));
            _componentList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            if (_settingObj.hasModifiedProperties)
                _settingObj.ApplyModifiedProperties();
        }
        
        private void DrawFile(FileInfo file, int tuck)
        {
            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = _operatingFile == file.FullName ? Color.green : Color.white;
            EditorGUILayout.LabelField("", GUILayout.Width(30 * tuck));
            EditorGUILayout.LabelField(file.Name);
                
            if (GUILayout.Button("打开PSD", GUILayout.Width(100)))
            {
                _operatingFile = file.FullName;
                CMDHelper.RunCmd($"explorer file:///{file.FullName}");
            }
            
            if (GUILayout.Button("预览", GUILayout.Width(100)))
            {
                _operatingFile = file.FullName;
                PSDPreviewWindow.ShowWin(GetParseInfo(file));
            }
            if (GUILayout.Button("生成", GUILayout.Width(100)))
            {
                if (!OpenNewScene())
                    return;
                _operatingFile = file.FullName;
                GenFile(file, false);
                PushInLatestOpened(file.DirectoryName);
            }

            EditorGUILayout.EndHorizontal();
        }

        public PSDInfo GetParseInfo(FileInfo file)
        {
            var info = new PSDParseInfo();
            info.Path = file.FullName;
            var fileName = Path.GetFileNameWithoutExtension(file.FullName);
            info.Name = fileName.Substring(fileName.IndexOf('@') + 1);
            var dir = new DirectoryInfo(_dirPath);
            info.PublicPath = file.FullName.Replace(dir.FullName, "")
                .Replace(file.Name, "").Replace('\\', '/').Trim('/');
            info.ToFolder = _toFolder;
            info.ImageSavePath = $"{_imgPath}/{info.PublicPath}";
            info.Setting = _setting;
            
            return new PSDInfo(info.Path, info);
        }
        
        private void GenFile(FileInfo file, bool exportImage)
        {
            PSDParse.Parse(GetParseInfo(file).ParseInfo, _setting.ScreenSize);
        }
        
        private void PushInLatestOpened(string dir)
        {
            _latestOpened.Remove(dir);
            _latestOpened.Insert(0, dir);
            if (_latestOpened.Count > 5)
                _latestOpened.RemoveAt(5);
        }

        private bool OpenNewScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "")
            {
                var res = EditorUtility.DisplayDialog("提示", "生成UI需要在空场景中进行，即将创建新场景，是否继续？", "继续", "取消");
                if (res)
                {
                    if (activeScene.isDirty)
                    {
                        var save = EditorUtility.DisplayDialog("提示", $"场景{activeScene.name}有更改，是否保存", "是", "否");
                        if (save)
                            EditorSceneManager.SaveScene(activeScene);
                    }
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                }
                return res;
            }

            return true;
        }
    }
}