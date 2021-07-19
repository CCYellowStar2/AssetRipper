﻿using AssetRipper.Classes;
using AssetRipper.Converters;
using System.Collections.Generic;

namespace AssetRipper.Project
{
	public interface IExportCollection
	{
		bool Export(ProjectAssetContainer container, string dirPath);
		bool IsContains(Object asset);
		long GetExportID(Object asset);
		MetaPtr CreateExportPointer(Object asset, bool isLocal);

		ISerializedFile File { get; }
		TransferInstructionFlags Flags { get; }
		IEnumerable<Object> Assets { get; }
		string Name { get; }
	}
}
