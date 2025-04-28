using System;
using UnityEngine;
using static Seb.Helpers.ColHelper;

namespace DLS.ColorStorage
{
    [System.Serializable]
    public struct ColorWithName
{
        
        public Color LowColor;
        public Color HighColor;
        public Color HoverColor;
        public string Name;

        public ColorWithName(Color lowColor, Color highColor, Color hoverColor, string name)
        {
            LowColor = lowColor;
            HighColor = highColor;
            HoverColor = hoverColor;
            Name = name;
        }
        public static ColorWithName Default => new ColorWithName(
                new Color(0.2f, 0.1f, 0.1f),  // LowColor
                new Color(0.95f, 0.3f, 0.31f), // HighColor
                Brighten(new Color(0.2f, 0.1f, 0.1f), 0.1f), // HoverColor
                "Red"
);
    }
}