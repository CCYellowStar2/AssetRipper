using AssetRipper.Classes;
using AssetRipper.Converters;
using System;
using Object = AssetRipper.Classes.Object;

namespace AssetRipper.Project
{
	public sealed class FontExportCollection : AssetExportCollection
	{
		public FontExportCollection(IAssetExporter assetExporter, Font asset) :
			base(assetExporter, asset)
		{
		}

		protected override string GetExportExtension(Object asset)
		{
			Font font = (Font)asset;
			byte[] fontData = (byte[])font.FontData;
			uint type = BitConverter.ToUInt32(fontData, 0);
			return type == OttoAsciiFourCC ? "otf" : "ttf";
		}

		/// <summary>
		/// OTTO ascii
		/// </summary>
		private const int OttoAsciiFourCC = 0x4F54544F;
	}
}
