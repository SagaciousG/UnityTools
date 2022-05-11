using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XGame
{
    public class PSDSetting : ScriptableObject
    {
        public string PsdFolder;
        public string UIFolder;
        public Vector2 ScreenSize = new Vector2(1920, 1080);
        public List<string> LatesdOpened = new List<string>();
        public Font DefaultFont;
        public List<string> ComponentTypes = new List<string>();
    }
}