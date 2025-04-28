using System;
using System.Linq;
using DLS.Description;
using DLS.Graphics;
using DLS.Simulation;
using UnityEngine;
using DLS.ColorStorage;
using System.Diagnostics;
using static Seb.Helpers.ColHelper;

namespace DLS.Game
{
	public class PinInstance : IInteractable
	{
		public readonly PinAddress Address;

		public readonly PinBitCount bitCount;
		public readonly bool IsBusPin;
		public readonly bool IsSourcePin;

		// Pin may be attached to a chip or a devPin as its parent
		public readonly IMoveable parent;
		public uint State; // sim state
		public uint PlayerInputState; // dev input pins only
		public ColorWithName Colour;
		bool faceRight;
		public float LocalPosY;
		public string Name;


		public PinInstance(PinDescription desc, PinAddress address, IMoveable parent, bool isSourcePin)
		{
			this.parent = parent;
			bitCount = desc.BitCount;
			Name = desc.Name;
			Address = address;
			IsSourcePin = isSourcePin;
			Colour = desc.Colour;

            IsBusPin = parent is SubChipInstance subchip && subchip.IsBus;
			faceRight = isSourcePin;
		}

		public Vector2 ForwardDir => faceRight ? Vector2.right : Vector2.left;


		public Vector2 GetWorldPos()
		{
			switch (parent)
			{
				case DevPinInstance devPin:
					return devPin.PinPosition;
				case SubChipInstance subchip:
				{
					Vector2 chipSize = subchip.Size;
					Vector2 chipPos = subchip.Position;

					float xLocal = (chipSize.x / 2 + DrawSettings.ChipOutlineWidth / 2 - DrawSettings.SubChipPinInset) * (faceRight ? 1 : -1);
					return chipPos + new Vector2(xLocal, LocalPosY);
				}
				default:
					throw new Exception("Parent type not supported");
			}
		}

		public void SetBusFlip(bool flipped)
		{
			faceRight = IsSourcePin ^ flipped;
		}
		public Color GetColLow(string test1)
		{      
            for (int i = 0; i < DLS.Graphics.ContextMenu.pinColors.Count; i++)
			{
                
                if (DLS.Graphics.ContextMenu.pinColors[i].Name == test1)
				{
					return DLS.Graphics.ContextMenu.pinColors[i].LowColor; 
                }
			}
            // If no matching color entry is found, return a default color
			return Color.black;
        }

        public Color GetColHigh(string test1)
        {
            for (int i = 0; i < DLS.Graphics.ContextMenu.pinColors.Count; i++)
            {
				
                if (DLS.Graphics.ContextMenu.pinColors[i].Name == test1)
				{ 
                    return DLS.Graphics.ContextMenu.pinColors[i].HighColor; 
                }

            }
            // If no matching color entry is found, return a default color
            return Color.black;
        }

        public Color GetStateCol(int bitIndex, bool hover = false, bool canUsePlayerState = true)
		{
			uint pinState = (IsSourcePin && canUsePlayerState) ? PlayerInputState : State; // dev input pin uses player state (so it updates even when sim is paused)

			uint state = PinState.GetBitTristatedValue(pinState, bitIndex);
            ColorWithName colorEntry = DLS.Graphics.ContextMenu.pinColors
        .FirstOrDefault(entry => entry.Name == Colour.Name);

			
            if (colorEntry.Equals(default(ColorWithName)))
            {
				// If no matching color entry is found, return a default color
				this.Colour = DLS.Graphics.ContextMenu.pinColors[0];
                return state switch
                {
                    PinState.LogicHigh => colorEntry.HighColor,
                    PinState.LogicLow => hover ? colorEntry.HoverColor : colorEntry.LowColor,
                    _ => DrawSettings.ActiveTheme.StateDisconnectedCol
                };
			}

            return state switch
			{
                PinState.LogicHigh => colorEntry.HighColor,
                PinState.LogicLow => hover ? colorEntry.HoverColor : colorEntry.LowColor,
                _ => DrawSettings.ActiveTheme.StateDisconnectedCol
            };
		}
	}
}