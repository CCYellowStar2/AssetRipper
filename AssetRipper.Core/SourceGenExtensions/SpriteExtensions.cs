﻿using AssetRipper.Core.Classes.Misc;
using AssetRipper.Core.Classes.Sprite;
using AssetRipper.Core.IO;
using AssetRipper.Core.Math.Colors;
using AssetRipper.Core.Math.Vectors;
using AssetRipper.Core.Project;
using AssetRipper.SourceGenerated.Classes.ClassID_213;
using AssetRipper.SourceGenerated.Classes.ClassID_687078895;
using AssetRipper.SourceGenerated.Subclasses.Rectf;
using AssetRipper.SourceGenerated.Subclasses.SpriteAtlasData;
using AssetRipper.SourceGenerated.Subclasses.SpriteBone;
using AssetRipper.SourceGenerated.Subclasses.SpriteMetaData;
using AssetRipper.SourceGenerated.Subclasses.Vector2f;
using AssetRipper.SourceGenerated.Subclasses.Vector4f;
using System.Buffers.Binary;

namespace AssetRipper.Core.SourceGenExtensions
{
	public static class SpriteExtensions
	{
		public static ISpriteMetaData GenerateSpriteMetaData(this ISprite sprite, IExportContainer container, ISpriteAtlas atlas)
		{
			sprite.GetSpriteCoordinatesInAtlas(atlas, out Rectf rect, out Vector2f_3_5_0_f5 pivot, out Vector4f_3_5_0_f5 border);

			ISpriteMetaData instance = SpriteMetaDataFactory.CreateAsset(container.ExportVersion);
			instance.NameString = sprite.NameString;
			instance.Rect.CopyValues(rect);
			instance.Alignment = (int)Classes.Meta.Importers.Texture.SpriteAlignment.Custom;
			instance.Pivot.CopyValues(pivot);
			instance.Border?.CopyValues(border);
			if (instance.Has_Outline())
			{
				sprite.GenerateOutline(container.Version, atlas, rect, pivot, instance.Outline);
			}
			if (instance.Has_PhysicsShape() && sprite.Has_PhysicsShape_C213())
			{
				sprite.GeneratePhysicsShape(atlas, rect, pivot, instance.PhysicsShape);
			}
			instance.TessellationDetail = 0;
			if (instance.Has_Bones() && sprite.Has_Bones_C213() && instance.Has_SpriteID())
			{
				// Scale bones based off of the sprite's PPU
				foreach (ISpriteBone bone in sprite.Bones_C213)
				{
					bone.Position.Scale(sprite.PixelsToUnits_C213);
					bone.Length *= sprite.PixelsToUnits_C213;

					// Set root bone position
					if (bone.ParentId == -1)
					{
						bone.Position.X += sprite.Rect_C213.Width / 2;
						bone.Position.Y += sprite.Rect_C213.Height / 2;
					}
				}

				instance.Bones.Clear();
				instance.Bones.Capacity = sprite.Bones_C213.Count;
				foreach (ISpriteBone bone in sprite.Bones_C213)
				{
					instance.Bones.AddNew().CopyValues(bone);
				}

				// NOTE: sprite ID is generated by sprite binary content, but we just generate a random value
				instance.SpriteID.String = Guid.NewGuid().ToString("N");

				instance.SetBoneGeometry(sprite);
			}
			return instance;
		}

		private static void SetBoneGeometry(this ISpriteMetaData instance, ISprite origin)
		{
			Vector3f[]? vertices = null;
			BoneWeights4[]? skin = null;

			origin.RD_C213.VertexData?.ReadData(origin.SerializedFile.Version, origin.SerializedFile.EndianType,
				out vertices,
				out Vector3f[]? _,//normals,
				out Vector4f[]? _,//tangents,
				out ColorRGBA32[]? _,//colors,
				out skin,
				out Vector2f[]? _,//uv0,
				out Vector2f[]? _,//uv1,
				out Vector2f[]? _,//uv2,
				out Vector2f[]? _,//uv3,
				out Vector2f[]? _,//uv4,
				out Vector2f[]? _,//uv5,
				out Vector2f[]? _,//uv6,
				out Vector2f[]? _);//uv7);

			if (instance.Has_Vertices())
			{
				instance.Vertices.Clear();

				// Convert Vector3f into Vector2f
				if (vertices is null)
				{
					instance.Vertices.Capacity = 0;
				}
				else
				{
					Vector2f_3_5_0_f5[] verts = new Vector2f_3_5_0_f5[vertices.Length];
					for (int i = 0; i < vertices.Length; i++)
					{
						verts[i] = new Vector2f_3_5_0_f5();

						verts[i].X = vertices[i].X;
						verts[i].Y = vertices[i].Y;

						// Scale and translate vertices properly
						verts[i].X *= origin.PixelsToUnits_C213;
						verts[i].Y *= origin.PixelsToUnits_C213;

						verts[i].X += origin.Rect_C213.Width / 2;
						verts[i].Y += origin.Rect_C213.Height / 2;
					}

					instance.Vertices.Capacity = verts.Length;
					instance.Vertices.AddRange(verts);
				}
			}

			if (!origin.RD_C213.Has_IndexBuffer() || origin.RD_C213.IndexBuffer.Length == 0)
			{
				instance.Indices = Array.Empty<int>();
			}
			else
			{
				instance.Indices = new int[origin.RD_C213.IndexBuffer.Length / 2];
				for (int i = 0, j = 0; i < origin.RD_C213.IndexBuffer.Length / 2; i++, j += 2)
				{
					instance.Indices[i] = BinaryPrimitives.ReadInt16LittleEndian(origin.RD_C213.IndexBuffer.AsSpan(j, 2));
				}
			}

#warning TODO: SpriteConverter does not generate instance.Edges

			if (instance.Has_Weights())
			{
				instance.Weights.Clear();
				if (skin is not null)
				{
					instance.Weights.EnsureCapacity(skin.Length);
					for (int i = 0; i < skin.Length; i++)
					{
						instance.Weights.Add((SourceGenerated.Subclasses.BoneWeights4.BoneWeights4_2017_1_0_b1)skin[i]);
					}
				}
			}
		}

		public static void GetSpriteCoordinatesInAtlas(this ISprite sprite, ISpriteAtlas? atlas, out Rectf sAtlasRect, out Vector2f_3_5_0_f5 sAtlasPivot, out Vector4f_3_5_0_f5 sAtlasBorder)
		{
			// sprite values are relative to original image (image, it was created from).
			// since atlas shuffle and crop sprite images, we need to recalculate those values.
			// if sprite doesn't belong to an atlas, consider its image as single sprite atlas

			Vector2f cropBotLeft;
			Vector2f cropTopRight;
			if (atlas is null || !sprite.Has_RenderDataKey_C213())
			{
				sAtlasRect = new();
				sAtlasRect.CopyValues(sprite.RD_C213.TextureRect);
				cropBotLeft = (Vector2f)sprite.RD_C213.TextureRectOffset;
			}
			else
			{
				ISpriteAtlasData atlasData = atlas.RenderDataMap_C687078895[sprite.RenderDataKey_C213];
				sAtlasRect = new();
				sAtlasRect.CopyValues(atlasData.TextureRect);
				cropBotLeft = (Vector2f)atlasData.TextureRectOffset;
			}

			Vector2f sizeDelta = sprite.Rect_C213.Size() - sAtlasRect.Size();
			cropTopRight = new Vector2f(sizeDelta.X - cropBotLeft.X, sizeDelta.Y - cropBotLeft.Y);

			Vector2f pivot;
			if (sprite.Has_Pivot_C213())
			{
				pivot = (Vector2f)sprite.Pivot_C213;
			}
			else
			{
				Vector2f center = new Vector2f(sprite.Rect_C213.Size().X / 2.0f, sprite.Rect_C213.Size().Y / 2.0f);
				Vector2f pivotOffset = center + (Vector2f)sprite.Offset_C213;
				pivot = new Vector2f(pivotOffset.X / sprite.Rect_C213.Size().X, pivotOffset.Y / sprite.Rect_C213.Size().Y);
			}

			Vector2f pivotPosition = new Vector2f(pivot.X * sprite.Rect_C213.Size().X, pivot.Y * sprite.Rect_C213.Size().Y);
			Vector2f aAtlasPivotPosition = pivotPosition - cropBotLeft;
			sAtlasPivot = new();
			sAtlasPivot.SetValues(aAtlasPivotPosition.X / sAtlasRect.Size().X, aAtlasPivotPosition.Y / sAtlasRect.Size().Y);

			sAtlasBorder = new();
			if (sprite.Has_Border_C213())
			{
				float borderL = sprite.Border_C213.X == 0.0f ? 0.0f : sprite.Border_C213.X - cropBotLeft.X;
				float borderB = sprite.Border_C213.Y == 0.0f ? 0.0f : sprite.Border_C213.Y - cropBotLeft.Y;
				float borderR = sprite.Border_C213.Z == 0.0f ? 0.0f : sprite.Border_C213.Z - cropTopRight.X;
				float borderT = sprite.Border_C213.W == 0.0f ? 0.0f : sprite.Border_C213.W - cropTopRight.Y;
				sAtlasBorder.SetValues(borderL, borderB, borderR, borderT);
			}
		}

		public static void GenerateOutline(
			this ISprite sprite,
			UnityVersion version,
			ISpriteAtlas atlas,
			Rectf rect,
			Vector2f_3_5_0_f5 pivot,
			AssetList<AssetList<Vector2f_3_5_0_f5>> outlines)
		{
			sprite.RD_C213.GenerateOutline(version, outlines);
			float pivotShiftX = (rect.Width * pivot.X) - (rect.Width * 0.5f);
			float pivotShiftY = (rect.Height * pivot.Y) - (rect.Height * 0.5f);
			Vector2f pivotShift = new Vector2f(pivotShiftX, pivotShiftY);
			foreach (AssetList<Vector2f_3_5_0_f5> outline in outlines)
			{
				for (int i = 0; i < outline.Count; i++)
				{
					Vector2f point = (Vector2f)outline[i] * sprite.PixelsToUnits_C213;
					outline[i] = (Vector2f_3_5_0_f5)(point + pivotShift);
				}
			}
			sprite.FixRotation(atlas, outlines);
		}

		public static void GeneratePhysicsShape(
			this ISprite sprite,
			ISpriteAtlas atlas,
			Rectf rect,
			Vector2f_3_5_0_f5 pivot,
			AssetList<AssetList<Vector2f_3_5_0_f5>> shape)
		{
			if (sprite.Has_PhysicsShape_C213() && sprite.PhysicsShape_C213.Count > 0)
			{
				shape.Clear();
				shape.Capacity = sprite.PhysicsShape_C213.Count;
				float pivotShiftX = (rect.Width * pivot.X) - (rect.Width * 0.5f);
				float pivotShiftY = (rect.Height * pivot.Y) - (rect.Height * 0.5f);
				Vector2f pivotShift = new Vector2f(pivotShiftX, pivotShiftY);
				for (int i = 0; i < sprite.PhysicsShape_C213.Count; i++)
				{
					shape.Add(new AssetList<Vector2f_3_5_0_f5>(sprite.PhysicsShape_C213[i].Count));
					for (int j = 0; j < sprite.PhysicsShape_C213[i].Count; j++)
					{
						Vector2f point = (Vector2f)sprite.PhysicsShape_C213[i][j] * sprite.PixelsToUnits_C213;
						shape[i].Add((Vector2f_3_5_0_f5)(point + pivotShift));
					}
				}
				sprite.FixRotation(atlas, shape);
			}
		}

		private static void FixRotation(this ISprite sprite, ISpriteAtlas atlas, AssetList<AssetList<Vector2f_3_5_0_f5>> outlines)
		{
			bool isPacked = sprite.RD_C213.IsPacked();
			SpritePackingRotation rotation = sprite.RD_C213.GetPackingRotation();
			if (atlas is not null && sprite.Has_RenderDataKey_C213())
			{
				ISpriteAtlasData atlasData = atlas.RenderDataMap_C687078895[sprite.RenderDataKey_C213];
				isPacked = atlasData.IsPacked();
				rotation = atlasData.GetPackingRotation();
			}

			if (isPacked)
			{
				switch (rotation)
				{
					case SpritePackingRotation.FlipHorizontal:
						{
							foreach (AssetList<Vector2f_3_5_0_f5> outline in outlines)
							{
								for (int i = 0; i < outline.Count; i++)
								{
									Vector2f_3_5_0_f5 vertex = outline[i];
									outline[i].SetValues(-vertex.X, vertex.Y);
								}
							}
						}
						break;

					case SpritePackingRotation.FlipVertical:
						{
							foreach (AssetList<Vector2f_3_5_0_f5> outline in outlines)
							{
								for (int i = 0; i < outline.Count; i++)
								{
									Vector2f_3_5_0_f5 vertex = outline[i];
									outline[i].SetValues(vertex.X, -vertex.Y);
								}
							}
						}
						break;

					case SpritePackingRotation.Rotate90:
						{
							foreach (AssetList<Vector2f_3_5_0_f5> outline in outlines)
							{
								for (int i = 0; i < outline.Count; i++)
								{
									Vector2f_3_5_0_f5 vertex = outline[i];
									outline[i].SetValues(vertex.Y, vertex.X);
								}
							}
						}
						break;

					case SpritePackingRotation.Rotate180:
						{
							foreach (AssetList<Vector2f_3_5_0_f5> outline in outlines)
							{
								for (int i = 0; i < outline.Count; i++)
								{
									Vector2f_3_5_0_f5 vertex = outline[i];
									outline[i].SetValues(-vertex.X, -vertex.Y);
								}
							}
						}
						break;
				}
			}
		}
	}
}
