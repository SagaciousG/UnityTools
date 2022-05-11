﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XGame
{
    public class PSDLayerGroup : IPSDLayer
    {
        public List<IPSDLayer> PsdLayers = new List<IPSDLayer>();

        public int LayerDividerIndex;

        public override void SetVariableValue(RectTransform transform)
        {
            if (IsRoot)
            {
                return;
            }
            foreach (var tag in Tags)
            {
               
            }

        }

        public override void SetDefaultValue(RectTransform obj)
        {
            
        }
        
        public override bool ValidTag(string tag)
        {
            if (tag.StartsWith("grid") || tag.StartsWith("ver") || tag.StartsWith("hor") 
                || tag.StartsWith("list="))
                return true;
            return base.ValidTag(tag);
        }
        
        
    }

    public enum PSDGroupFlag
    {
        None,
        GridLayout,
        HorizontalLayout,
        VerticalLayout,
    }
}