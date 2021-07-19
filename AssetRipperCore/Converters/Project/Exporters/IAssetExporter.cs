using AssetRipper.Project;
using AssetRipper.SerializedFiles;
using System;
using System.Collections.Generic;
using Object = AssetRipper.Classes.Object;

namespace AssetRipper.Converters
{
	public interface IAssetExporter
	{
		bool IsHandle(Object asset, ExportOptions options);

		bool Export(IExportContainer container, Object asset, string path);
		void Export(IExportContainer container, Object asset, string path, Action<IExportContainer, Object, string> callback);
		bool Export(IExportContainer container, IEnumerable<Object> assets, string path);
		void Export(IExportContainer container, IEnumerable<Object> assets, string path, Action<IExportContainer, Object, string> callback);

		IExportCollection CreateCollection(VirtualSerializedFile virtualFile, Object asset);
		AssetType ToExportType(Object asset);
		bool ToUnknownExportType(ClassIDType classID, out AssetType assetType);
	}
}
