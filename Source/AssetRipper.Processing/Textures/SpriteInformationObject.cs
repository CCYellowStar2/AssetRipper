using AssetRipper.Assets;
using AssetRipper.Assets.Metadata;
using AssetRipper.Assets.Traversal;
using AssetRipper.SourceGenerated.Classes.ClassID_213;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Classes.ClassID_687078895;
using System.Diagnostics;
using System.Reflection;

namespace AssetRipper.Processing.Textures;

public sealed class SpriteInformationObject : AssetGroup, INamed
{
    // 缓存反射字段信息，避免重复查找
    private static FieldInfo? _masterAtlasField;

    public SpriteInformationObject(AssetInfo assetInfo, ITexture2D texture) : base(assetInfo)
    {
        Texture = texture;
    }

    public ITexture2D Texture { get; }
    public IReadOnlyDictionary<ISprite, ISpriteAtlas?> Sprites => dictionary;
    private readonly Dictionary<ISprite, ISpriteAtlas?> dictionary = new();

    Utf8String INamed.Name
    {
        get => Texture.Name;
        set { }
    }

    public override IEnumerable<IUnityObjectBase> Assets
    {
        get
        {
            yield return Texture;
            foreach ((ISprite sprite, ISpriteAtlas? atlas) in dictionary)
            {
                yield return sprite;
                if (atlas is not null)
                {
                    yield return atlas;
                }
            }
        }
    }

    public override void WalkStandard(AssetWalker walker)
    {
        if (walker.EnterAsset(this))
        {
            this.WalkPPtrField(walker, Texture);
            walker.DivideAsset(this);
            this.WalkDictionaryPPtrField(walker, Sprites);
            walker.ExitAsset(this);
        }
    }

    public override IEnumerable<(string, PPtr)> FetchDependencies()
    {
        yield return (nameof(Texture), AssetToPPtr(Texture));
        foreach ((ISprite sprite, ISpriteAtlas? atlas) in dictionary)
        {
            yield return (nameof(Sprites) + "[].Key", AssetToPPtr(sprite));
            if (atlas is not null)
            {
                yield return (nameof(Sprites) + "[].Value", AssetToPPtr(atlas));
            }
        }
    }

    internal void AddToDictionary(ISprite sprite, ISpriteAtlas? atlas)
    {
        if (dictionary.TryGetValue(sprite, out ISpriteAtlas? mappedAtlas))
        {
            if (mappedAtlas is null)
            {
                dictionary[sprite] = atlas;
            }
            else if (atlas is not null)
            {
                // 使用反射动态获取 MasterAtlas 字段值
                object? atlasMaster = GetMasterAtlas(atlas);
                object? mappedAtlasMaster = GetMasterAtlas(mappedAtlas);

                if (atlasMaster != null && !atlasMaster.Equals(mappedAtlasMaster))
                {
                    throw new Exception($"{nameof(atlas)} is not the same as {nameof(mappedAtlas)}");
                }
            }
        }
        else
        {
            dictionary.Add(sprite, atlas);
        }
    }

    private static object? GetMasterAtlas(ISpriteAtlas atlas)
    {
        // 延迟初始化字段信息
        if (_masterAtlasField == null)
        {
            _masterAtlasField = FindMasterAtlasField(atlas.GetType());
            if (_masterAtlasField == null)
            {
                throw new NotSupportedException($"Failed to find MasterAtlas field in {atlas.GetType()}");
            }
        }

        return _masterAtlasField.GetValue(atlas);
    }

    private static FieldInfo? FindMasterAtlasField(Type atlasType)
    {
        // 匹配字段名规则：masterAtlas_C + 8位十六进制（如 masterAtlas_C12345678）
        const string prefix = "masterAtlas_C";
        foreach (FieldInfo field in atlasType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (field.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) 
                && field.Name.Length == prefix.Length + 8) // 8位哈希
            {
                return field;
            }
        }
        return null;
    }

    public override void SetMainAsset()
    {
        Debug.Assert(Texture.MainAsset is null);
        base.SetMainAsset();
    }
}
