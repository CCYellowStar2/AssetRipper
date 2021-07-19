﻿using AssetRipper.Classes;
using AssetRipper.Converters;
using AssetRipper.YAML;

namespace AssetRipper.Classes.Misc
{
	public struct FixedBitset : IAssetReadable, IYAMLExportable
	{
		public void Read(AssetReader reader)
		{
			Data = reader.ReadUInt32Array();
			reader.AlignStream();
		}

		public YAMLNode ExportYAML(IExportContainer container)
		{
			YAMLMappingNode node = new YAMLMappingNode();
			node.Add(DataName, Data.ExportYAML(true));
			return node;
		}

		public uint[] Data { get; set; }

		public const string DataName = "data";
	}
}
