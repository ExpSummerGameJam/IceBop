using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;

public class ParsingPerformanceTest : MonoBehaviour
{
    private String2TypeByteConverter converter;

    private void Start()
    {
        converter = new String2TypeByteConverter();

        // 테스트 데이터 세트
        var testValues = new[]
        {
            ("123", "int"),
            ("123.456", "float"),
            ("1.0,2.0", "vector2"),
            ("1.0,2.0,3.0", "vector3"),
            ("0.0,0.0,0.0,1.0", "quaternion"),
            ("1.0,0.5,0.3,1.0", "color"),
            ("invalid", "int"),
            ("not_a_number", "float"),
            ("1.0,2.0", "vector3"),
            ("true", "bool"),
            ("invalid_bool", "bool")
        };

        int[] testCounts = { 1000, 10000, 100000, 500000 };

        foreach (int count in testCounts)
        {
            UnityEngine.Debug.Log($"--- Running tests with {count} iterations ---");

            MeasurePerformance(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    foreach (var (value, type) in testValues)
                    {
                        try
                        {
                            _ = converter.Convert(type, value);
                        }
                        catch
                        {
                            // 예외 무시
                        }
                    }
                }
            }, $"String2TypeByteConverter ({count} iterations)");

            MeasurePerformance(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    foreach (var (value, type) in testValues)
                    {
                        try
                        {
                            byte[] result;
                            switch (type)
                            {
                                case "int":
                                    result = ParseIntWithFallback(value, 0);
                                    break;
                                case "float":
                                    result = ParseFloatWithFallback(value, 0f);
                                    break;
                                case "vector2":
                                    result = ParseVector2WithFallback(value, Vector2.zero);
                                    break;
                                case "vector3":
                                    result = ParseVector3WithFallback(value, Vector3.zero);
                                    break;
                                case "quaternion":
                                    result = ParseQuaternionWithFallback(value, Quaternion.identity);
                                    break;
                                case "color":
                                    result = ParseColorWithFallback(value, Color.white);
                                    break;
                                case "bool":
                                    result = ParseBoolWithFallback(value, false);
                                    break;
                                default:
                                    throw new NotSupportedException($"Type {type} is not supported.");
                            }
                        }
                        catch
                        {
                            // 예외 무시
                        }
                    }
                }
            }, $"Manual Parsing with Binary Conversion ({count} iterations)");
        }
    }

    private void MeasurePerformance(Action testAction, string testName)
    {
        long initialMemory = Profiler.GetTotalAllocatedMemoryLong();
        Stopwatch stopwatch = Stopwatch.StartNew();

        testAction.Invoke();

        stopwatch.Stop();
        long finalMemory = Profiler.GetTotalAllocatedMemoryLong();
        long memoryUsed = finalMemory - initialMemory;

        UnityEngine.Debug.Log($"{testName}: {stopwatch.ElapsedMilliseconds} ms, Memory Used: {memoryUsed} bytes");
    }

    private byte[] ParseIntWithFallback(string value, int defaultValue)
    {
        int result = int.TryParse(value, out int parsed) ? parsed : defaultValue;
        return BitConverter.GetBytes(result);
    }

    private byte[] ParseFloatWithFallback(string value, float defaultValue)
    {
        float result = float.TryParse(value, out float parsed) ? parsed : defaultValue;
        return BitConverter.GetBytes(result);
    }

    private byte[] ParseVector2WithFallback(string value, Vector2 defaultValue)
    {
        string[] parts = value.Split(',');
        Vector2 result = defaultValue;
        if (parts.Length == 2 &&
            float.TryParse(parts[0], out float x) &&
            float.TryParse(parts[1], out float y))
        {
            result = new Vector2(x, y);
        }
        return ConvertVector2ToBytes(result);
    }

    private byte[] ParseVector3WithFallback(string value, Vector3 defaultValue)
    {
        string[] parts = value.Split(',');
        Vector3 result = defaultValue;
        if (parts.Length == 3 &&
            float.TryParse(parts[0], out float x) &&
            float.TryParse(parts[1], out float y) &&
            float.TryParse(parts[2], out float z))
        {
            result = new Vector3(x, y, z);
        }
        return ConvertVector3ToBytes(result);
    }

    private byte[] ParseQuaternionWithFallback(string value, Quaternion defaultValue)
    {
        string[] parts = value.Split(',');
        Quaternion result = defaultValue;
        if (parts.Length == 4 &&
            float.TryParse(parts[0], out float x) &&
            float.TryParse(parts[1], out float y) &&
            float.TryParse(parts[2], out float z) &&
            float.TryParse(parts[3], out float w))
        {
            result = new Quaternion(x, y, z, w);
        }
        return ConvertQuaternionToBytes(result);
    }

    private byte[] ParseColorWithFallback(string value, Color defaultValue)
    {
        string[] parts = value.Split(',');
        Color result = defaultValue;
        if (parts.Length == 4 &&
            float.TryParse(parts[0], out float r) &&
            float.TryParse(parts[1], out float g) &&
            float.TryParse(parts[2], out float b) &&
            float.TryParse(parts[3], out float a))
        {
            result = new Color(r, g, b, a);
        }
        return ConvertColorToBytes(result);
    }

    private byte[] ParseBoolWithFallback(string value, bool defaultValue)
    {
        bool result = value.Equals("true", StringComparison.OrdinalIgnoreCase) ? true :
                      value.Equals("false", StringComparison.OrdinalIgnoreCase) ? false :
                      defaultValue;
        return BitConverter.GetBytes(result);
    }

    private byte[] ConvertVector2ToBytes(Vector2 vector)
    {
        var bytes = new byte[8];
        Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, 4, 4);
        return bytes;
    }

    private byte[] ConvertVector3ToBytes(Vector3 vector)
    {
        var bytes = new byte[12];
        Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, 8, 4);
        return bytes;
    }

    private byte[] ConvertQuaternionToBytes(Quaternion quaternion)
    {
        var bytes = new byte[16];
        Buffer.BlockCopy(BitConverter.GetBytes(quaternion.x), 0, bytes, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(quaternion.y), 0, bytes, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(quaternion.z), 0, bytes, 8, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(quaternion.w), 0, bytes, 12, 4);
        return bytes;
    }

    private byte[] ConvertColorToBytes(Color color)
    {
        var bytes = new byte[16];
        Buffer.BlockCopy(BitConverter.GetBytes(color.r), 0, bytes, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(color.g), 0, bytes, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(color.b), 0, bytes, 8, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(color.a), 0, bytes, 12, 4);
        return bytes;
    }
}
