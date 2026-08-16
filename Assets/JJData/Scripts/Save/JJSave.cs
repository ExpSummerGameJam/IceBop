using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using CompressionLevel = System.IO.Compression.CompressionLevel;

public static class JJSave
{
    public static readonly string DataFilePath = "JJSave/";
    public static readonly string Extension = ".bytes";

    private static string GetResourcesPath(string fileName, string fileRoot)
    {
        return $"{fileRoot}{fileName}";
    }

    private static string GetStreamingAssetsPath(string fileName, string fileRoot)
    {
        return $"{Application.streamingAssetsPath}/{fileRoot}{fileName}{Extension}";
    }

    #region util
    public static string GetSavePath(string fileName, string fileRoot)
    {
        StringBuilder sb = new();

#if UNITY_ANDROID && !UNITY_EDITOR
        sb.Append(Application.persistentDataPath);
        sb.Append("/");
        sb.Append(fileRoot);
#elif UNITY_EDITOR
        sb.Append(Application.dataPath);
        sb.Append("/");
        sb.Append(fileRoot);
#elif UNITY_STANDALONE_WIN
        sb.Append(Application.persistentDataPath);
        sb.Append("/");
        sb.Append(fileRoot);
#endif

        CheckSavePath(sb.ToString());

        sb.Append($"{fileName}{Extension}");
        return sb.ToString();
    }

    private static void CheckSavePath(string dirPath)
    {
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
    }

    public static byte[] Compress(byte[] input)
    {
        using MemoryStream output = new();
        using (GZipStream gzip = new(output, CompressionLevel.Optimal))
        {
            gzip.Write(input, 0, input.Length);
        }
        return output.ToArray();
    }

    public static byte[] Decompress(byte[] input)
    {
        using MemoryStream inputStream = new(input);
        using GZipStream gzip = new(inputStream, CompressionMode.Decompress);
        using MemoryStream output = new();
        gzip.CopyTo(output);
        return output.ToArray();
    }
    #endregion

    #region Save&Load - Resources
    public static void RSave<T>(T target, string saveDataName, string fileRoot, bool compress = true)
    {
#if !UNITY_EDITOR
        // Resources에 저장하는건 Editor 상으로 제한. 빌드 후에는 Load만 가능
        return;
#endif

        ReadOnlySpan<byte> typeByte;
        if (compress)
            typeByte = Compress(Type2TypeByteConverter.Convert(target));
        else
            typeByte = Type2TypeByteConverter.Convert(target);

        string path = GetSavePath(saveDataName, $"Resources/{fileRoot}");
        if (File.Exists(path))
            File.Delete(path);
        using var fileStream = new FileStream(path, FileMode.Create);
        try
        {
            fileStream.Write(typeByte);
            Debug.Log($"JJSave : Data saved to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"JJSave : Failed to save data - {e}");
        }
        finally
        {
            fileStream.Close();
        }
    }

    // out을 이용해서 반환 값을 받는다.
    // T 명시를 생략하기 위해서임.
    public static void RLoad<T>(out T result, string saveDataName, string fileRoot, bool compress = true)
    {
        result = RLoad<T>(saveDataName, fileRoot, compress);
    }

    // 반환 값으로 Load 값을 받는다.
    public static T RLoad<T>(string saveDataName, string fileRoot, bool compress = true)
    {
        string path = GetResourcesPath(saveDataName, fileRoot);
        T result = default;

        try
        {
            TextAsset textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null)
            {
                Debug.LogWarning($"JJSave : save file Not Find: {path}");
                return result;
            }
            byte[] data = textAsset.bytes;
            if (data == null || data.Length == 0)
            {
                Debug.LogWarning($"JJSave : save file Error: {path}");
                return result;
            }
            if (compress)
                result = TypeByte2TypeConverter.Convert<T>(Decompress(data));
            else
                result = TypeByte2TypeConverter.Convert<T>(data);
            Debug.Log($"JJSave : Data loaded from {path}");

            Resources.UnloadAsset(textAsset);
        }
        catch (Exception e)
        {
            Debug.LogError($"JJSave : Failed to load data - {e}");
        }


        return result;
    }
    #endregion

    #region Save&Load - Assets
    public static void ASave<T>(T target, string saveDataName, string fileRoot, bool compress = true)
    {
#if !UNITY_EDITOR
        // Assets에 저장하는건 Editor 상으로 제한. Adressables 전용임.
        // 따라서 Load는 존재안함.
        return;
#endif

        ReadOnlySpan<byte> typeByte;
        if (compress)
            typeByte = Compress(Type2TypeByteConverter.Convert(target));
        else
            typeByte = Type2TypeByteConverter.Convert(target);

        string path = GetSavePath(saveDataName, fileRoot);
        if (File.Exists(path))
            File.Delete(path);
        using var fileStream = new FileStream(path, FileMode.Create);
        try
        {
            fileStream.Write(typeByte);
            Debug.Log($"JJSave : Data saved to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"JJSave : Failed to save data - {e}");
        }
        finally
        {
            fileStream.Close();
        }
    }
    #endregion

    #region Save&Load - Save
    public static void LSave<T>(T target, string saveDataName, string fileRoot = "", bool compress = true)
    {
        ReadOnlySpan<byte> typeByte;
        if (compress)
            typeByte = Compress(Type2TypeByteConverter.Convert(target));
        else
            typeByte = Type2TypeByteConverter.Convert(target);

        string path = GetSavePath(saveDataName, $"{DataFilePath}{fileRoot}");
        if (File.Exists(path))
            File.Delete(path);
        try
        {
            using var fileStream = new FileStream(path, FileMode.Create);
            fileStream.Write(typeByte);
            Debug.Log($"JJSave : Data saved to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"JJSave : Failed to save data - {e}");
        }
    }

    // out을 이용해서 반환 값을 받는다.
    // T 명시를 생략하기 위해서임.
    public static void LLoad<T>(out T result,  string saveDataName, string fileRoot = "", bool compress = true)
    {
        result = LLoad<T>(saveDataName, fileRoot, compress);
    }

    // 반환 값으로 Load 값을 받는다.
    public static T LLoad<T>(string saveDataName, string fileRoot = "", bool compress = true)
    {
        string path = GetSavePath(saveDataName, $"{DataFilePath}{fileRoot}");
        T result = default;

        if (!File.Exists(path))
        {
            Debug.LogWarning($"JJSave : No save file found at: {path}");
            return result;
        }

        try
        {
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            long length = fileStream.Length;

            if (length <= 0 || length > int.MaxValue)
            {
                // 이 함수는 Save로 저장한 파일들만 읽어오는 Load 함수이다.
                // Save 단계에서 길이가 벗어나는 파일은 이미 걸러지기에
                // Load 단계에서 길이가 벗어난다면 무조건 잘못 된 파일이다.
                // 따라서 제외한다.
                throw new IOException("JJSave : Invalid or corrupted file.");
            }

            byte[] buffer = new byte[length];
            int totalRead = 0;
            while (totalRead < length)
            {
                int read = fileStream.Read(buffer, totalRead, (int)length - totalRead);
                if (read == 0)
                    throw new IOException("JJSave : Unexpected end of file.");
                totalRead += read;
            }
            if (compress)
                result = TypeByte2TypeConverter.Convert<T>(Decompress(buffer));
            else
                result = TypeByte2TypeConverter.Convert<T>(buffer);
            Debug.Log($"JJSave : Data loaded from {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"JJSave : Failed to load data - {e}");
        }

        return result;
    }
    #endregion
}
