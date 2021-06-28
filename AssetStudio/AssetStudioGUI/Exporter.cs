using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using AssetStudio;
using Newtonsoft.Json;
using TGASharpLib;
using System.Security.Cryptography;

namespace AssetStudioGUI
{
    internal static class Exporter
    {
        public static bool ExportTexture2D(AssetItem item, string exportPath, out string exportFullPath)
        {
            var m_Texture2D = (Texture2D)item.Asset;
            if (Properties.Settings.Default.convertTexture)
            {
                var bitmap = m_Texture2D.ConvertToBitmap(true);
                if (bitmap == null)
                {
                    exportFullPath = "";
                    return false;
                }
                ImageFormat format = null;
                var ext = Properties.Settings.Default.convertType;
                bool tga = false;
                switch (ext)
                {
                    case "BMP":
                        format = ImageFormat.Bmp;
                        break;
                    case "PNG":
                        format = ImageFormat.Png;
                        break;
                    case "JPEG":
                        format = ImageFormat.Jpeg;
                        break;
                    case "TGA":
                        tga = true;
                        break;
                }
                if (!TryExportFile(exportPath, item, "." + ext.ToLower(), out exportFullPath))
                {
                    return false;
                }
                if (tga)
                {
                    var file = new TGA(bitmap);
                    file.Save(exportFullPath);
                }
                else
                {
                    bitmap.Save(exportFullPath, format);
                }
                bitmap.Dispose();

                return true;
            }
            else
            {
                if (!TryExportFile(exportPath, item, ".tex", out exportFullPath))
                    return false;
                File.WriteAllBytes(exportFullPath, m_Texture2D.image_data.GetData());

                return true;
            }
        }

        public static bool ExportAudioClip(AssetItem item, string exportPath)
        {
            var m_AudioClip = (AudioClip)item.Asset;
            var m_AudioData = m_AudioClip.m_AudioData.GetData();
            if (m_AudioData == null || m_AudioData.Length == 0)
                return false;
            var converter = new AudioClipConverter(m_AudioClip);
            if (Properties.Settings.Default.convertAudio && converter.IsSupport)
            {
                if (!TryExportFile(exportPath, item, ".wav", out var exportFullPath))
                    return false;
                var buffer = converter.ConvertToWav();
                if (buffer == null)
                    return false;
                File.WriteAllBytes(exportFullPath, buffer);
            }
            else
            {
                if (!TryExportFile(exportPath, item, converter.GetExtensionName(), out var exportFullPath))
                    return false;
                File.WriteAllBytes(exportFullPath, m_AudioData);
            }
            return true;
        }

        public static bool ExportShader(AssetItem item, string exportPath)
        {
            if (!TryExportFile(exportPath, item, ".shader", out var exportFullPath))
                return false;
            var m_Shader = (Shader)item.Asset;
            var str = m_Shader.Convert();
            File.WriteAllText(exportFullPath, str);
            return true;
        }

        public static bool ExportTextAsset(AssetItem item, string exportPath)
        {
            var m_TextAsset = (TextAsset)(item.Asset);
            var extension = ".txt";
            if (Properties.Settings.Default.restoreExtensionName)
            {
                if (!string.IsNullOrEmpty(item.Container))
                {
                    extension = Path.GetExtension(item.Container);
                }
            }
            if (!TryExportFile(exportPath, item, extension, out var exportFullPath))
                return false;
            File.WriteAllBytes(exportFullPath, m_TextAsset.m_Script);
            return true;
        }

        public static bool ExportMonoBehaviour(AssetItem item, string exportPath)
        {
            if (!TryExportFile(exportPath, item, ".json", out var exportFullPath))
                return false;
            var m_MonoBehaviour = (MonoBehaviour)item.Asset;
            var type = m_MonoBehaviour.ToType();
            if (type == null)
            {
                var nodes = Studio.MonoBehaviourToTypeTreeNodes(m_MonoBehaviour);
                type = m_MonoBehaviour.ToType(nodes);
            }
            var str = JsonConvert.SerializeObject(type, Formatting.Indented);
            File.WriteAllText(exportFullPath, str);
            return true;
        }

        public static bool ExportFont(AssetItem item, string exportPath, out string exportFullPath)
        {
            exportFullPath = "";
            var m_Font = (Font)item.Asset;
            if (m_Font.m_FontData != null)
            {
                var extension = ".ttf";
                if (m_Font.m_FontData[0] == 79 && m_Font.m_FontData[1] == 84 && m_Font.m_FontData[2] == 84 && m_Font.m_FontData[3] == 79)
                {
                    extension = ".otf";
                }
                if (!TryExportFile(exportPath, item, extension, out exportFullPath))
                    return false;
                File.WriteAllBytes(exportFullPath, m_Font.m_FontData);
                return true;
            }
            return false;
        }

        public static bool ExportMesh(AssetItem item, string exportPath, out string exportFullPath)
        {
            exportFullPath = "";
            var m_Mesh = (Mesh)item.Asset;
            if (m_Mesh.m_VertexCount <= 0)
                return false;
            if (!TryExportFile(exportPath, item, ".obj", out exportFullPath))
                return false;
            var sb = new StringBuilder();
            sb.AppendLine("g " + m_Mesh.m_Name);
            #region Vertices
            if (m_Mesh.m_Vertices == null || m_Mesh.m_Vertices.Length == 0)
            {
                return false;
            }
            int c = 3;
            if (m_Mesh.m_Vertices.Length == m_Mesh.m_VertexCount * 4)
            {
                c = 4;
            }
            for (int v = 0; v < m_Mesh.m_VertexCount; v++)
            {
                sb.AppendFormat("v {0} {1} {2}\r\n", -m_Mesh.m_Vertices[v * c], m_Mesh.m_Vertices[v * c + 1], m_Mesh.m_Vertices[v * c + 2]);
            }
            #endregion

            #region UV
            if (m_Mesh.m_UV0?.Length > 0)
            {
                if (m_Mesh.m_UV0.Length == m_Mesh.m_VertexCount * 2)
                {
                    c = 2;
                }
                else if (m_Mesh.m_UV0.Length == m_Mesh.m_VertexCount * 3)
                {
                    c = 3;
                }
                for (int v = 0; v < m_Mesh.m_VertexCount; v++)
                {
                    sb.AppendFormat("vt {0} {1}\r\n", m_Mesh.m_UV0[v * c], m_Mesh.m_UV0[v * c + 1]);
                }
            }
            #endregion

            #region Normals
            if (m_Mesh.m_Normals?.Length > 0)
            {
                if (m_Mesh.m_Normals.Length == m_Mesh.m_VertexCount * 3)
                {
                    c = 3;
                }
                else if (m_Mesh.m_Normals.Length == m_Mesh.m_VertexCount * 4)
                {
                    c = 4;
                }
                for (int v = 0; v < m_Mesh.m_VertexCount; v++)
                {
                    sb.AppendFormat("vn {0} {1} {2}\r\n", -m_Mesh.m_Normals[v * c], m_Mesh.m_Normals[v * c + 1], m_Mesh.m_Normals[v * c + 2]);
                }
            }
            #endregion

            #region Face
            int sum = 0;
            for (var i = 0; i < m_Mesh.m_SubMeshes.Length; i++)
            {
                sb.AppendLine($"g {m_Mesh.m_Name}_{i}");
                int indexCount = (int)m_Mesh.m_SubMeshes[i].indexCount;
                var end = sum + indexCount / 3;
                for (int f = sum; f < end; f++)
                {
                    sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\r\n", m_Mesh.m_Indices[f * 3 + 2] + 1, m_Mesh.m_Indices[f * 3 + 1] + 1, m_Mesh.m_Indices[f * 3] + 1);
                }
                sum = end;
            }
            #endregion

            sb.Replace("NaN", "0");
            File.WriteAllText(exportFullPath, sb.ToString());
            return true;
        }

        public static bool ExportVideoClip(AssetItem item, string exportPath)
        {
            var m_VideoClip = (VideoClip)item.Asset;
            var m_VideoData = m_VideoClip.m_VideoData.GetData();
            if (m_VideoData != null && m_VideoData.Length != 0)
            {
                if (!TryExportFile(exportPath, item, Path.GetExtension(m_VideoClip.m_OriginalPath), out var exportFullPath))
                    return false;
                File.WriteAllBytes(exportFullPath, m_VideoData);
                return true;
            }
            return false;
        }

        public static bool ExportMovieTexture(AssetItem item, string exportPath)
        {
            var m_MovieTexture = (MovieTexture)item.Asset;
            if (!TryExportFile(exportPath, item, ".ogv", out var exportFullPath))
                return false;
            File.WriteAllBytes(exportFullPath, m_MovieTexture.m_MovieData);
            return true;
        }

        public static bool ExportSprite(AssetItem item, string exportPath)
        {
            ImageFormat format = null;
            var type = Properties.Settings.Default.convertType;
            bool tga = false;
            switch (type)
            {
                case "BMP":
                    format = ImageFormat.Bmp;
                    break;
                case "PNG":
                    format = ImageFormat.Png;
                    break;
                case "JPEG":
                    format = ImageFormat.Jpeg;
                    break;
                case "TGA":
                    tga = true;
                    break;
            }
            if (!TryExportFile(exportPath, item, "." + type.ToLower(), out var exportFullPath))
                return false;
            var bitmap = ((Sprite)item.Asset).GetImage();
            if (bitmap != null)
            {
                if (tga)
                {
                    var file = new TGA(bitmap);
                    file.Save(exportFullPath);
                }
                else
                {
                    bitmap.Save(exportFullPath, format);
                }
                bitmap.Dispose();
                return true;
            }
            return false;
        }

        public static bool ExportRawFile(AssetItem item, string exportPath, out string exportFullPath)
        {
            if (!TryExportFile(exportPath, item, ".dat", out exportFullPath))
                return false;
            File.WriteAllBytes(exportFullPath, item.Asset.GetRawData());
            return true;
        }

        private static bool TryExportFile(string dir, AssetItem item, string extension, out string fullPath)
        {
            var fileName = FixFileName(item.Text);
            fileName = fileName.Replace("(", "_");
            fileName = fileName.Replace(")", "_");
            fileName = fileName.Replace(" ", "_");
            fullPath = Path.Combine(dir, fileName + extension);
            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            fullPath = Path.Combine(dir, fileName + item.UniqueID + extension);
            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            return false;
        }

        public static bool ExportAnimator(AssetItem item, string exportPath, List<AssetItem> animationList = null)
        {
            var exportFullPath = Path.Combine(exportPath, item.Text, item.Text + ".fbx");
            if (File.Exists(exportFullPath))
            {
                exportFullPath = Path.Combine(exportPath, item.Text + item.UniqueID, item.Text + ".fbx");
            }
            var m_Animator = (Animator)item.Asset;
            var convert = animationList != null
                ? new ModelConverter(m_Animator, Properties.Settings.Default.convertType, animationList.Select(x => (AnimationClip)x.Asset).ToArray())
                : new ModelConverter(m_Animator, Properties.Settings.Default.convertType);
            ExportFbx(convert, exportFullPath);
            return true;
        }

        public static void ExportGameObject(GameObject gameObject, string exportPath, List<AssetItem> animationList = null)
        {
            var convert = animationList != null
                ? new ModelConverter(gameObject, Properties.Settings.Default.convertType, animationList.Select(x => (AnimationClip)x.Asset).ToArray())
                : new ModelConverter(gameObject, Properties.Settings.Default.convertType);
            exportPath = exportPath + FixFileName(gameObject.m_Name) + ".fbx";
            ExportFbx(convert, exportPath);
        }

        public static void ExportGameObjectMerge(List<GameObject> gameObject, string exportPath, List<AssetItem> animationList = null)
        {
            var rootName = Path.GetFileNameWithoutExtension(exportPath);
            var convert = animationList != null
                ? new ModelConverter(rootName, gameObject, Properties.Settings.Default.convertType, animationList.Select(x => (AnimationClip)x.Asset).ToArray())
                : new ModelConverter(rootName, gameObject, Properties.Settings.Default.convertType);
            ExportFbx(convert, exportPath);
        }

        private static void ExportFbx(IImported convert, string exportPath)
        {
            var eulerFilter = Properties.Settings.Default.eulerFilter;
            var filterPrecision = (float)Properties.Settings.Default.filterPrecision;
            var exportAllNodes = Properties.Settings.Default.exportAllNodes;
            var exportSkins = Properties.Settings.Default.exportSkins;
            var exportAnimations = Properties.Settings.Default.exportAnimations;
            var exportBlendShape = Properties.Settings.Default.exportBlendShape;
            var castToBone = Properties.Settings.Default.castToBone;
            var boneSize = (int)Properties.Settings.Default.boneSize;
            var scaleFactor = (float)Properties.Settings.Default.scaleFactor;
            var fbxVersion = Properties.Settings.Default.fbxVersion;
            var fbxFormat = Properties.Settings.Default.fbxFormat;
            ModelExporter.ExportFbx(exportPath, convert, eulerFilter, filterPrecision,
                exportAllNodes, exportSkins, exportAnimations, exportBlendShape, castToBone, boneSize, scaleFactor, fbxVersion, fbxFormat == 1);
        }

        public static bool ExportDumpFile(AssetItem item, string exportPath)
        {
            if (!TryExportFile(exportPath, item, ".txt", out var exportFullPath))
                return false;
            var str = item.Asset.Dump();
            if (str == null && item.Asset is MonoBehaviour m_MonoBehaviour)
            {
                var nodes = Studio.MonoBehaviourToTypeTreeNodes(m_MonoBehaviour);
                str = m_MonoBehaviour.Dump(nodes);
            }
            if (str != null)
            {
                File.WriteAllText(exportFullPath, str);
                return true;
            }
            return false;
        }

        public static bool ExportConvertFile(AssetItem item, string exportPath)
        {
            string exportFullPath;
            switch (item.Type)
            {
                case ClassIDType.Texture2D:
                    return ExportTexture2D(item, exportPath, out exportFullPath);
                case ClassIDType.AudioClip:
                    return ExportAudioClip(item, exportPath);
                case ClassIDType.Shader:
                    return ExportShader(item, exportPath);
                case ClassIDType.TextAsset:
                    return ExportTextAsset(item, exportPath);
                case ClassIDType.MonoBehaviour:
                    return ExportMonoBehaviour(item, exportPath);
                case ClassIDType.Font:
                    return ExportFont(item, exportPath, out exportFullPath);
                case ClassIDType.Mesh:
                    return ExportMesh(item, exportPath, out exportFullPath);
                case ClassIDType.VideoClip:
                    return ExportVideoClip(item, exportPath);
                case ClassIDType.MovieTexture:
                    return ExportMovieTexture(item, exportPath);
                case ClassIDType.Sprite:
                    return ExportSprite(item, exportPath);
                case ClassIDType.Animator:
                    return ExportAnimator(item, exportPath);
                case ClassIDType.AnimationClip:
                    return false;
                default:
                    return ExportRawFile(item, exportPath, out exportFullPath);
            }
        }

        public static bool ExportVizFile(AssetItem item, string savePath, StreamWriter csvFile)
        {
            bool result = true;
            string filename = "";
            string hash = "";
            string dimension = "";
            string format = "";
            byte[] rawData = null;
            var sourcePath = savePath.Replace("-pkg", "\\");
            var exportPath = Path.Combine(savePath, item.TypeString);

            switch (item.Type)
            {
                case ClassIDType.Texture2D:
                    {
                        var texture2D = (Texture2D)item.Asset;
                        if (texture2D.m_MipMap)
                            dimension = string.Format("{0}x{1} mips", texture2D.m_Width, texture2D.m_Height, texture2D.m_MipCount);
                        else
                            dimension = string.Format("{0}x{1}", texture2D.m_Width, texture2D.m_Height);
                        if (texture2D.m_Width >= 512 || texture2D.m_Height >= 512)
                        {
                            result = ExportTexture2D(item, exportPath, out filename);
                            filename = filename.Replace(exportPath, "Texture2D/");
                        }
                        else
                        {
                            rawData = texture2D.image_data.GetData();
                        }
                        format = texture2D.m_TextureFormat.ToString();
                        break;
                    }
                case ClassIDType.Texture2DArray:
                    {
                        rawData = item.Asset.GetRawData();
                        //result = ExportRawFile(item, exportPath, out filename);
                        //filename = filename.Replace(exportPath, "Texture2DArray/");
                        break;
                    }
                case ClassIDType.Shader:
                    {
                        rawData = item.Asset.GetRawData();
                        //result = ExportRawFile(item, exportPath, out filename);
                        var shader = (Shader)item.Asset;
                        //filename = filename.Replace(exportPath, "Shader/");
                        break;
                    }
                case ClassIDType.Font:
                    {
                        //rawData = item.Asset.GetRawData();
                        //result = ExportFont(item, exportPath, out filename);
                        //filename = filename.Replace(exportPath, "Font/");
                        //result = ExportRawFile(item, exportPath, out filename);
                        var font = (Font)item.Asset;
                        rawData = font.m_FontData;
                        //filename = filename.Replace(exportPath, "Font/");
                        break;
                    }
                case ClassIDType.Mesh:
                    {
                        var mesh = (Mesh)item.Asset;
                        if (mesh.m_VertexCount > 1000)
                        {
                            result = ExportMesh(item, exportPath, out filename);
                            filename = filename.Replace(exportPath, "Mesh/");
                        }
                        else
                        {
                            rawData = item.Asset.GetRawData();
                        }
                        //PreviewAsset()
                        //result = ExportRawFile(item, exportPath, out filename);
                        dimension = string.Format("vtx:{0} idx:{1} uv:{2} n:{3}", 
                            mesh.m_VertexCount, mesh.m_Indices.Count, mesh.m_UV0?.Length, mesh.m_Normals?.Length);
                        //filename = filename.Replace(exportPath, "Mesh/");
                        break;
                    }
                case ClassIDType.TextAsset:
                    {
                        rawData = item.Asset.GetRawData();
                        break;
                    }
                case ClassIDType.AudioClip:
                    {
                        rawData = item.Asset.GetRawData();
                        //result = ExportRawFile(item, exportPath, out filename);
                        var audioClip = (AudioClip)item.Asset;
                        //filename = filename.Replace(exportPath, "AudioClip/");
                        break;
                    }
                case ClassIDType.AnimationClip:
                    {
                        rawData = item.Asset.GetRawData();
                        //result = ExportRawFile(item, exportPath, out filename);
                        var animationClip = (AnimationClip)item.Asset;
                        //filename = filename.Replace(exportPath, "AnimationClip/");
                        break;
                    }

                default:
                    return false;
            }

            if (rawData != null)
            {
                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] retVal = md5.ComputeHash(rawData);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < retVal.Length; i++)
                    {
                        sb.Append(retVal[i].ToString("x2"));
                    }
                    hash = sb.ToString();
                }
            }
            item.Text = item.Text.Replace("\0", "");
            //csvFile.Write("Name,Container,Type,Dimension,Format,Size,FileName,Hash,OriginalFile\n");
            var originalFile = item.SourceFile.originalPath ?? item.SourceFile.fullName;
            originalFile = originalFile.Replace(sourcePath, "");
            originalFile = originalFile.Replace("\\", "/");
            csvFile.Write(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\n",
                item.Text, item.Container, item.TypeString, dimension, format, item.FullSize, filename, hash, originalFile));

            return result;
        }

        public static string FixFileName(string str)
        {
            if (str.Length >= 260) return Path.GetRandomFileName();
            return Path.GetInvalidFileNameChars().Aggregate(str, (current, c) => current.Replace(c, '_'));
        }
    }
}
