using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UIElements;
using System.Buffers.Binary;

public static class Type2TypeByteConverter
{
    // 변환 델리게이트 캐시: object를 입력받아 byte[]를 반환하는 함수
    private static readonly Dictionary<Type, Func<object, byte[]>> _convertDelegateCache = new Dictionary<Type, Func<object, byte[]>>();
    // 직렬화 대상 필드 캐시
    private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new Dictionary<Type, FieldInfo[]>();

    private static bool _serializeReference = false;

    public static byte[] Convert<T>(T value)
    {
        if (value == null)
            return Array.Empty<byte>();

        Type type = typeof(T);

        if (type == typeof(string))
            return Convertstring2Byte(value as string);

        if (type == typeof(Texture2D))
            return ConvertTexture2D2Byte(value as Texture2D);

        if (type.IsArray)
            return ConvertArray2Byte((Array)(object)value);

        if (type.IsGenericType)
        {
            Type genericTypeDef = type.GetGenericTypeDefinition();

            if (genericTypeDef == typeof(List<>))
                return ConvertList2Byte((IList)(object)value, type.GetGenericArguments()[0]);

            if (genericTypeDef == typeof(Dictionary<,>))
                return ConvertDictionary2Byte((IDictionary)(object)value, type.GetGenericArguments()[0], type.GetGenericArguments()[1]);

            if (genericTypeDef == typeof(HashSet<>))
                return ConvertHashSet2Byte((IEnumerable)(object)value, type.GetGenericArguments()[0]);
        }

        if (type.IsPrimitive)
            return ConvertPrimitive2Byte(value);

        if (type.IsEnum)
            return ConvertEnum2Byte(value);

        if (type.IsValueType)
            return ConvertStruct2Byte(value);

        if (type.IsClass)
            return ConvertClass2Byte(value);

        throw new NotSupportedException($"[{type.FullName}] 타입은 직렬화 지원되지 않습니다.");
    }

    /// <summary>
    /// object를 입력받아 해당 타입에 맞게 변환하는 델리게이트를 생성 및 캐싱합니다.
    /// </summary>
    private static Func<object, byte[]> GetConvertDelegate(Type type)
    {
        if (!_convertDelegateCache.TryGetValue(type, out var converter))
        {
            MethodInfo method = typeof(Type2TypeByteConverter).GetMethod(nameof(Convert), BindingFlags.Public | BindingFlags.Static);
            MethodInfo genericMethod = method.MakeGenericMethod(type);

            ParameterExpression param = Expression.Parameter(typeof(object), "value");
            UnaryExpression castParam = Expression.Convert(param, type);
            MethodCallExpression call = Expression.Call(genericMethod, castParam);
            converter = Expression.Lambda<Func<object, byte[]>>(call, param).Compile();
            _convertDelegateCache[type] = converter;
        }
        return converter;
    }

    /// <summary>
    /// 캐싱된 직렬화 대상 필드들을 가져옵니다.
    /// </summary>
    private static FieldInfo[] GetSerializableFields(Type type)
    {
        if (!_fieldCache.TryGetValue(type, out var fields))
        {
            const BindingFlags flags = BindingFlags.Instance
                                     | BindingFlags.Public
                                     | BindingFlags.NonPublic
                                     | BindingFlags.DeclaredOnly;

            var list = new List<FieldInfo>();

            for (var t = type; t != null; t = t.BaseType)
            {
                foreach (var f in t.GetFields(flags))
                {
                    if (f.IsNotSerialized)
                        continue;
                    if (f.IsPublic
                        || f.GetCustomAttribute<SerializeField>(inherit: true) != null)
                    {
                        list.Add(f);
                    }
                }
            }

            fields = list.ToArray();
            _fieldCache[type] = fields;
        }
        return fields;
    }

    public static byte[] Convertstring2Byte(string value)
    {
        if (value == null) value = "";
        Span<byte> stringByte = Encoding.UTF8.GetBytes(value);
        int length = stringByte.Length;
        Span<byte> lengthByte = stackalloc byte[4];
        lengthByte = ConvertPrimitive2Byte(length);

        Span<byte> result = stackalloc byte[4 + length];
        lengthByte.CopyTo(result);
        stringByte.CopyTo(result.Slice(4));

        return result.ToArray();
    }

    // Texture는 크기가 크므로 스택이 아닌 힙 메모리 사용
    public static byte[] ConvertTexture2D2Byte(Texture2D texture)
    {
        List<byte> result = new();

        result.AddRange(Convert(texture.width));
        result.AddRange(Convert(texture.height));
        result.AddRange(Convert(texture.format.ToString()));
        result.AddRange(Convert(texture.mipmapCount));

        byte[] rawData = texture.GetRawTextureData();
        result.AddRange(Convert(rawData.Length));
        result.AddRange(rawData);

        return result.ToArray();
    }

    public static byte[] ConvertEnum2Byte<T>(T value)
    {
        return ConvertPrimitive2Byte(value.GetHashCode());
    }

    public static byte[] ConvertPrimitive2Byte(object value)
    {
        if (value is short s)
        {
            byte[] buffer = new byte[2];
            BinaryPrimitives.WriteInt16LittleEndian(buffer, s);
            return buffer;
        }
        else if (value is ushort us)
        {
            byte[] buffer = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, us);
            return buffer;
        }
        else if (value is int i)
        {
            byte[] buffer = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, i);
            return buffer;
        }
        else if (value is uint ui)
        {
            byte[] buffer = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, ui);
            return buffer;
        }
        else if (value is long l)
        {
            byte[] buffer = new byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, l);
            return buffer;
        }
        else if (value is ulong ul)
        {
            byte[] buffer = new byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, ul);
            return buffer;
        }
        else if (value is float f)
        {
            int bits = BitConverter.SingleToInt32Bits(f);
            byte[] buffer = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, bits);
            return buffer;
        }
        else if (value is double d)
        {
            long bits = BitConverter.DoubleToInt64Bits(d);
            byte[] buffer = new byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, bits);
            return buffer;
        }
        else if (value is bool b)
        {
            // bool은 1바이트로 처리 (true: 1, false: 0)
            return new byte[] { (byte)(b ? 1 : 0) };
        }
        else if (value is char c)
        {
            byte[] buffer = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, c);
            return buffer;
        }
        else
        {
            // 안전한 fallback: 기존의 Marshal 방식을 사용 (단, 위에서 처리하지 못한 경우)
            Type type = value.GetType();
            int size = Marshal.SizeOf(type);
            byte[] buffer = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(value, ptr, false);
                Marshal.Copy(ptr, buffer, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return buffer;
        }
    }

    public static byte[] ConvertStruct2Byte(object value)
    {
        // ValueType는 동일한 방식으로 처리합니다.
        return ConvertPrimitive2Byte(value);
    }

    public static byte[] ConvertArray2Byte(Array array)
    {
        List<byte> result = new();
        int length = array.Length;
        result.AddRange(ConvertPrimitive2Byte(length));

        if (length == 0)
            return result.ToArray();

        if (_serializeReference)
        {
            string typeName;
            Type realType;
            foreach (var item in array)
            {
                typeName = item.GetType().AssemblyQualifiedName;
                realType = Type.GetType(typeName);
                result.AddRange(Convertstring2Byte(typeName));
                var converter = GetConvertDelegate(realType);
                result.AddRange(converter(item));
            }
        }
        else
        {
            Type elementType = array.GetType().GetElementType();
            var converter = GetConvertDelegate(elementType);
            foreach (var item in array)
            {
                result.AddRange(converter(item));
            }
        }
        return result.ToArray();
    }

    public static byte[] ConvertList2Byte(IList list, Type elementType)
    {
        List<byte> result = new();
        int count = list.Count;
        result.AddRange(ConvertPrimitive2Byte(count));

        if (count == 0)
            return result.ToArray();

        if (_serializeReference)
        {
            string typeName;
            Type realType;
            foreach (var item in list)
            {
                typeName = item.GetType().AssemblyQualifiedName;
                realType = Type.GetType(typeName);
                result.AddRange(Convertstring2Byte(typeName));
                var converter = GetConvertDelegate(realType);
                result.AddRange(converter(item));
            }
        }
        else
        {
            var converter = GetConvertDelegate(elementType);
            foreach (var item in list)
            {
                result.AddRange(converter(item));
            }
        }
        return result.ToArray();
    }

    public static byte[] ConvertDictionary2Byte(IDictionary dict, Type keyType, Type valueType)
    {
        List<byte> result = new();
        int count = dict.Count;
        result.AddRange(ConvertPrimitive2Byte(count));

        if (count == 0)
            return result.ToArray();

        var keyConverter = GetConvertDelegate(keyType);
        var valueConverter = GetConvertDelegate(valueType);
        foreach (DictionaryEntry entry in dict)
        {
            result.AddRange(keyConverter(entry.Key));
            result.AddRange(valueConverter(entry.Value));
        }
        return result.ToArray();
    }

    public static byte[] ConvertHashSet2Byte(IEnumerable set, Type elementType)
    {
        List<byte> result = new();
        int count = set.Cast<object>().Count();
        result.AddRange(ConvertPrimitive2Byte(count));

        if (count == 0)
            return result.ToArray();

        var converter = GetConvertDelegate(elementType);
        foreach (var item in set)
        {
            result.AddRange(converter(item));
        }
        return result.ToArray();
    }

    public static byte[] ConvertClass2Byte<T>(T obj)
    {
        List<byte> result = new();
        Type type = typeof(T);

        FieldInfo[] fields = GetSerializableFields(type);
        foreach (var field in fields)
        {
            object fieldValue = field.GetValue(obj);
            if (fieldValue == null)
            {
                result.AddRange(ConvertPrimitive2Byte(false));
                continue;
            }
            result.AddRange(ConvertPrimitive2Byte(true));

            if (field.GetCustomAttribute<SerializeReference>() != null)
            {
                _serializeReference = true;
                Type fieldType = fieldValue.GetType();
                string typeName = fieldType.AssemblyQualifiedName;
                result.AddRange(Convertstring2Byte(typeName));
                Type realType = Type.GetType(typeName);

                var converter = GetConvertDelegate(realType);
                result.AddRange(converter(fieldValue));
                _serializeReference = false;
            }
            else
            {
                bool flag = false;
                if (_serializeReference)
                    flag = true;
                _serializeReference = false;
                Type fieldType = field.FieldType;
                var converter = GetConvertDelegate(fieldType);
                result.AddRange(converter(fieldValue));
                _serializeReference = flag;
            }
        }
        return result.ToArray();
    }
}
