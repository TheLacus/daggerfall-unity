// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2019 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: TheLacus
// Contributors:
// 
// Notes:
//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DaggerfallWorkshop.Utility
{
    /// <summary>
    /// An helper class for <see cref="Texture2DArray"/> creation.
    /// </summary>
    public static class TextureArrayBuilder
    {
        /// <summary>
        /// A performant copy of texture data on gpu-side which doesn't require readable flag to be set but is not supported on all platforms.
        /// </summary>
        public static bool CopyTextureSupported
        {
            get { return (SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) == CopyTextureSupport.DifferentTypes; }
        }

        /// <summary>
        /// Makes a texture array from a collection of individual textures with the same size and format.
        /// </summary>
        /// <param name="textures">A collection of textures with the same size and format.</param>
        /// <param name="fallbackColor">If provided is used silently for textures that equal null in the collection.</param>
        /// <param name="makeNoLongerReadable">If false it performs creation with a slower method that requires readable flag on source textures.</param>
        /// <param name="name">The name to assign to texture array, also useful for debug.</param>
        /// <returns>The created texture array or null if texture arrays are not supported.</returns>
        public static Texture2DArray Make(IList<Texture2D> textures, Color32? fallbackColor = null, bool makeNoLongerReadable = true, string name = null)
        {
            if (!SystemInfo.supports2DArrayTextures)
                return null;

            Texture2DArray textureArray = null;
            bool mipMaps = false;
            Texture2D fallback = null;

            bool useCopyTexture = makeNoLongerReadable && CopyTextureSupported;

            for (int layer = 0; layer < textures.Count; layer++)
            {
                Texture2D tex = textures[layer];
                if (!tex)
                {
                    if (!textureArray)
                        return null;

                    if (!fallbackColor.HasValue)
                    {
                        Debug.LogErrorFormat("Failed to inject layer {0} inside texture archive {1} because texture data is not available.", layer, textureArray.name);
                        continue;
                    }

                    if (!fallback)
                        fallback = GetFallbackTexture(textureArray.width, textureArray.height, textureArray.format, mipMaps, fallbackColor.Value);

                    tex = fallback;
                }

                if (!textureArray)
                {
                    textureArray = new Texture2DArray(tex.width, tex.height, textures.Count, tex.format, mipMaps = tex.mipmapCount > 1);
                    if (name != null)
                        textureArray.name = name;
                    if (useCopyTexture)
                        textureArray.Apply(false, true);
                }

                if (tex.width != textureArray.width && tex.height != textureArray.height && tex.format != textureArray.format)
                {
                    Debug.LogErrorFormat("Failed to inject {0} as layer {1} of texture archive {2} due to size or format mismatch.", layer, tex.name, textureArray.name);
                    continue;
                }

                if (useCopyTexture)
                    Graphics.CopyTexture(tex, 0, textureArray, layer);
                else
                    textureArray.SetPixels32(tex.GetPixels32(), layer);
            }

            if (fallback)
                Texture2D.Destroy(fallback);

            if (textureArray && !useCopyTexture)
                textureArray.Apply(true, makeNoLongerReadable);

            return textureArray;
        }

        private static Texture2D GetFallbackTexture(int width, int height, TextureFormat format, bool mipMaps, Color32 color)
        {
            var tex = new Texture2D(width, height, format, mipMaps);
            Color32[] colors = new Color32[tex.width * tex.height];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = color;
            tex.SetPixels32(colors);
            tex.Apply(mipMaps, true);
            return tex;
        }
    }
}
