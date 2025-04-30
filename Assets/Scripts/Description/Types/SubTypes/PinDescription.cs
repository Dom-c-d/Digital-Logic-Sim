using UnityEngine;
using DLS.ColorStorage;
using System.Collections.Generic;

namespace DLS.Description
{
	public struct PinDescription
	{
		public string Name;
		public int ID;
		public Vector2 Position;
		public PinBitCount BitCount;
        public string Colour;
		public PinValueDisplayMode ValueDisplayMode;

		public PinDescription(string name, int id, Vector2 position, PinBitCount bitCount, string colour, PinValueDisplayMode valueDisplayMode)
		{
			Name = name;
			ID = id;
			Position = position;
			BitCount = bitCount;
			Colour = colour;
			ValueDisplayMode = valueDisplayMode;
		}
	}

	public enum PinBitCount
	{
		Bit1 = 1,
		Bit4 = 4,
		Bit8 = 8
	}


	public enum PinValueDisplayMode
	{
		Off,
		UnsignedDecimal,
		SignedDecimal,
		HEX
	}
}