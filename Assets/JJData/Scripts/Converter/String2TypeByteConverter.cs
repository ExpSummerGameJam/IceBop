using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

public partial class String2TypeByteConverter
{
    private readonly Dictionary<string, Func<string, byte[]>> _converters = new();
    public static Dictionary<string, int> TypeByteLength = new();

    public String2TypeByteConverter()
    {
        AutoConverter();
    }

    private void AutoConverter()
    {
        string[] defaultConveter = new string[] { "string", "int", "float", "double", "long", "short", "bool", "char", "Vector2", "Vector3", "Quaternion", "Color" };
        Dictionary<string, int> defaultTypeByteLength = new()
        {
            {"string", -1},
            {"int", 4},
            {"float", 4},
            {"double", 8},
            {"long", 8},
            {"short", 2},
            {"bool", 1},
            {"char", 1},
            {"Vector2", Marshal.SizeOf<Vector2>()},
            {"Vector3", Marshal.SizeOf<Vector3>()},
            {"Quaternion", Marshal.SizeOf<Quaternion>()},
            {"Color", Marshal.SizeOf<Color>()},
        };

        foreach(var typeByte in defaultTypeByteLength)
        {
            if (!TypeByteLength.ContainsKey(typeByte.Key))
                TypeByteLength[typeByte.Key] = typeByte.Value;
        }

        MethodInfo[] converters = typeof(String2TypeByteConverter).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (MethodInfo converter in converters)
        {
            string converterName = converter.Name;
            if (converterName.StartsWith("Convert2") && converterName.EndsWith("Byte") && converter.ReturnType == typeof(byte[]))
            {
                string typeName = converterName.Substring("Convert2".Length, converterName.Length - "Convert2".Length - "Byte".Length);
                if (defaultConveter.Contains(typeName))
                    continue;
                if(!TypeByteLength.ContainsKey(typeName))
                {
                    Type type = Type.GetType(typeName, throwOnError: false);
                    TypeByteLength[typeName] = Marshal.SizeOf(type);
                }
                _converters[typeName] = (Func<string, byte[]>)Delegate.CreateDelegate(
                typeof(Func<string, byte[]>), this, converter);
            }
        }
    }

    public byte[] Convert(string type, string value)
    {
        switch (type.AsSpan())
        {
            case var t when t.Equals("string".AsSpan(), StringComparison.CurrentCulture):
                return Convert2stringByte(value);
            case var t when t.Equals("int".AsSpan(), StringComparison.CurrentCulture):
                return Convert2intByte(value);
            case var t when t.Equals("float".AsSpan(), StringComparison.CurrentCulture):
                return Convert2floatByte(value);
            case var t when t.Equals("double".AsSpan(), StringComparison.CurrentCulture):
                return Convert2doubleByte(value);
            case var t when t.Equals("long".AsSpan(), StringComparison.CurrentCulture):
                return Convert2longByte(value);
            case var t when t.Equals("short".AsSpan(), StringComparison.CurrentCulture):
                return Convert2shortByte(value);
            case var t when t.Equals("bool".AsSpan(), StringComparison.CurrentCulture):
                return Convert2boolByte(value);
            case var t when t.Equals("char".AsSpan(), StringComparison.CurrentCulture):
                return Convert2charByte(value);
            case var t when t.Equals("Vector2".AsSpan(), StringComparison.CurrentCulture):
                return Convert2Vector2Byte(value);
            case var t when t.Equals("Vector3".AsSpan(), StringComparison.CurrentCulture):
                return Convert2Vector3Byte(value);
            case var t when t.Equals("Quaternion".AsSpan(), StringComparison.CurrentCulture):
                return Convert2QuaternionByte(value);
            case var t when t.Equals("Color".AsSpan(), StringComparison.CurrentCulture):
                return Convert2ColorByte(value);
        }

        if (_converters.TryGetValue(type, out var converter))
            return converter(value);

        throw new NotSupportedException($"<{type}> 형태의 Converter는 지원안합니다. 따로 추가해주십시오.");
    }
    #region ConverterDefualt
    public int IntDefault = 0;
    public float FloatDefault = 0f;
    public double DoubleDefault = 0f;
    public long LongDefault = 0;
    public short ShortDefault = 0;
    public string CharDefault = "";
    public bool BoolDefault = false;
    public Vector2 Vector2Default = Vector2.zero;
    public Vector3 Vector3Default = Vector3.zero;
    public Quaternion QuaternionDefault = Quaternion.identity;
    public Color ColorDefault = Color.white;
    #endregion
    #region Converter
    private byte[] Convert2stringByte(string value)
    {
        Span<byte> stringByte = Encoding.UTF8.GetBytes(value);
        int length = stringByte.Length;
        Span<byte> lengthByte = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref length, 1));

        Span<byte> result = stackalloc byte[4 + length];

        // 문자열 데이터 복사
        lengthByte.CopyTo(result);
        stringByte.CopyTo(result.Slice(4));

        return result.ToArray();
    }
    private byte[] Convert2intByte(string value)
    {
        return ConvertToBytes(value, IntDefault, int.TryParse, 4);
    }
    private byte[] Convert2floatByte(string value)
    {
        return ConvertToBytes(value, FloatDefault, float.TryParse, 4);
    }
    private byte[] Convert2doubleByte(string value)
    {
        return ConvertToBytes(value, DoubleDefault, double.TryParse, 8);
    }
    private byte[] Convert2longByte(string value)
    {
        return ConvertToBytes(value, LongDefault, long.TryParse, 8);
    }
    private byte[] Convert2shortByte(string value)
    {
        return ConvertToBytes(value, ShortDefault, short.TryParse, 2);
    }
    private byte[] ConvertToBytes<T>(string value, T defaultValue, TryParseDelegate<T> parser, int size) where T : struct
    {
        if (!parser(value, out T result))
            result = defaultValue;
        Span<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref result, 1));
        return bytes.ToArray();
    }
    private delegate bool TryParseDelegate<T>(string input, out T result);
    private byte[] Convert2charByte(string value)
    {
        return Encoding.UTF8.GetBytes(value.Length == 1 ? value : CharDefault);
    }
    private byte[] Convert2boolByte(string value)
    {
        Span<byte> bytes = stackalloc byte[1];
        bytes[0] = (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1") ? (byte)1 :
                   (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value == "0") ? (byte)0 :
                   (byte)(BoolDefault ? 1 : 0);
        return bytes.ToArray();
    }
    // CostomConverter
    private static readonly Dictionary<Type, int> StructSizes = new();
    private int GetStructSize<T>() where T : struct
    {
        Type type = typeof(T);
        if (!StructSizes.TryGetValue(type, out int size))
        {
            size = Marshal.SizeOf(type);
            StructSizes[type] = size;
        }
        return size;
    }

    private byte[] SerializeStruct<T>(T value) where T : struct
    {
        int size = GetStructSize<T>();
        Span<byte> bytes = stackalloc byte[size];
        MemoryMarshal.Write(bytes, ref value);
        return bytes.ToArray();
    }

    private byte[] Convert2Vector2Byte(string value)
    {
        float[] defaultValues = { Vector2Default.x, Vector2Default.y };
        if (!TryParseComponents(value, 2, out float[] components, defaultValues))
        {
            return SerializeStruct(Vector2Default);
        }

        return SerializeStruct(new Vector2(components[0], components[1]));
    }

    private byte[] Convert2Vector3Byte(string value)
    {
        float[] defaultValues = { Vector3Default.x, Vector3Default.y, Vector3Default.z };
        if (!TryParseComponents(value, 3, out float[] components, defaultValues))
        {
            return SerializeStruct(Vector3Default);
        }

        return SerializeStruct(new Vector3(components[0], components[1], components[2]));
    }

    private byte[] Convert2QuaternionByte(string value)
    {
        float[] defaultValues = { QuaternionDefault.x, QuaternionDefault.y, QuaternionDefault.z, QuaternionDefault.w };
        if (!TryParseComponents(value, 4, out float[] components, defaultValues))
        {
            return SerializeStruct(QuaternionDefault);
        }

        return SerializeStruct(new Quaternion(components[0], components[1], components[2], components[3]));
    }

    private byte[] Convert2ColorByte(string value)
    {
        float[] defaultValues = { ColorDefault.r, ColorDefault.g, ColorDefault.b, ColorDefault.a };
        if (!TryParseComponents(value, 4, out float[] components, defaultValues))
        {
            return SerializeStruct(ColorDefault);
        }

        return SerializeStruct(new Color(components[0], components[1], components[2], components[3]));
    }

    private bool TryParseComponents(string value, int expectedCount, out float[] result, float[] defaultValues)
    {
        result = new float[expectedCount];
        var span = value.AsSpan();

        for (int i = 0, start = 0; i < expectedCount; i++)
        {
            int commaIndex = span.Slice(start).IndexOf(',');
            if (commaIndex == -1) commaIndex = span.Length - start;

            if (!float.TryParse(span.Slice(start, commaIndex), out result[i]))
            {
                Array.Copy(defaultValues, result, expectedCount);
                return false;
            }

            start += commaIndex + 1;
            if (start >= span.Length) break;
        }

        return true;
    }
    #endregion
}