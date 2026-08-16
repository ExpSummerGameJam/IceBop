using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using System.Runtime.Serialization;

public partial class TypeByte2TypeConverter
{
    private static bool _serializeReference = false;

    public static string ConvertTypeByte2string(ReadOnlySpan<byte> bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }
    public static int ConvertTypeByte2int(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Read<int>(bytes);
    }

    public static float ConvertTypeByte2float(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Read<float>(bytes);
    }

    public static double ConvertTypeByte2double(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Read<double>(bytes);
    }

    public static long ConvertTypeByte2long(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Read<long>(bytes);
    }

    public static short ConvertTypeByte2short(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Read<short>(bytes);
    }

    public static bool ConvertTypeByte2bool(ReadOnlySpan<byte> bytes)
    {
        return bytes[0] == 1;
    }

    public static char ConvertTypeByte2char(ReadOnlySpan<byte> bytes)
    {
        return (char)bytes[0];
    }

    public static Vector2 ConvertTypeByte2Vector2(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Read<Vector2>(bytes);
    }

    public static Vector3 ConvertTypeByte2Vector3(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Read<Vector3>(bytes);
    }

    public static Quaternion ConvertTypeByte2Quaternion(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Read<Quaternion>(bytes);
    }

    public static Color ConvertTypeByte2Color(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Read<Color>(bytes);
    }


    private static readonly Dictionary<Type, MethodInfo> _convertMethodCache = new();
    private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new();
    private static readonly Dictionary<(Type, string), Action<object, object>> _fieldSetterCache = new Dictionary<(Type, string), Action<object, object>>();
    private static Dictionary<Type, ConvertDelegate> _convertDelegateCache = new();
    private delegate object ConvertDelegate(byte[] data, ref int offset);


    public static T Convert<T>(byte[] data)
    {

        int offset = 0;
        return Convert<T>(data, ref offset);
    }

    public static T Convert<T>(byte[] data, ref int offset)
    {
        Type type = typeof(T);

        if (type == typeof(string))
            return (T)(object)ConvertTypeByte2LenString(data, ref offset);

        if (type == typeof(Texture2D))
            return (T)(object)ConvertTypeByte2Texture2D(data, ref offset);

        if (type.IsArray)
            return (T)(object)ConvertTypeByte2Array(data, ref offset, type.GetElementType());

        if (type.IsEnum)
            return (T)ConvertTypeByte2Enum(data, ref offset, type);

        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();

            if (genericTypeDef == typeof(List<>))
                return (T)ConvertTypeByte2List(data, ref offset, type.GetGenericArguments()[0]);

            if (genericTypeDef == typeof(Dictionary<,>))
                return (T)ConvertTypeByte2Dictionary(data, ref offset, type.GetGenericArguments()[0], type.GetGenericArguments()[1]);

            if (genericTypeDef == typeof(HashSet<>))
                return (T)ConvertTypeByte2HashSet(data, ref offset, type.GetGenericArguments()[0]);
        }

        if (type.IsPrimitive)
            return ConvertTypeByte2Primitive<T>(data, ref offset);

        if (type.IsEnum)
            return (T)ConvertTypeByte2Enum(data, ref offset, type);

        if (type.IsValueType)
            return (T)ConvertTypeByte2Struct(data, ref offset, type);

        if (type.IsClass)
            return (T)ConvertTypeByte2Class(data, ref offset, type);

        throw new NotSupportedException($"[{type.FullName}] 타입은 역직렬화 지원되지 않습니다.");
    }

    public static string ConvertTypeByte2LenString(byte[] data, ref int offset)
    {
        int length = ConvertTypeByte2Primitive<int>(data, ref offset);
        string result = Encoding.UTF8.GetString(data, offset, length);
        offset += length;
        return result;
    }

    public static Texture2D ConvertTypeByte2Texture2D(byte[] data, ref int offset)
    {
        int width = ConvertTypeByte2Primitive<int>(data, ref offset);
        int height = ConvertTypeByte2Primitive<int>(data, ref offset);
        string format = ConvertTypeByte2LenString(data, ref offset);
        int mipmapCount = ConvertTypeByte2Primitive<int>(data, ref offset);
        int rawDataLength = ConvertTypeByte2Primitive<int>(data, ref offset);

        byte[] rawData = new byte[rawDataLength];
        Buffer.BlockCopy(data, offset, rawData, 0, rawDataLength);
        offset += rawDataLength;

        TextureFormat texFormat = (TextureFormat)Enum.Parse(typeof(TextureFormat), format);
        Texture2D tex = new Texture2D(width, height, texFormat, mipmapCount > 1);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.LoadRawTextureData(rawData);
        tex.Apply();

        return tex;
    }

    public static object ConvertTypeByte2Enum(byte[] data, ref int offset, Type type)
    {
        int enumValue = ConvertTypeByte2Primitive<int>(data, ref offset);
        return Enum.ToObject(type, enumValue);
    }

    public static T ConvertTypeByte2Primitive<T>(byte[] data, ref int offset)
    {
        ReadOnlySpan<byte> span = data.AsSpan(offset);
        T result;

        if (typeof(T) == typeof(short))
        {
            result = (T)(object)BinaryPrimitives.ReadInt16LittleEndian(span);
            offset += sizeof(short);
        }
        else if (typeof(T) == typeof(int))
        {
            result = (T)(object)BinaryPrimitives.ReadInt32LittleEndian(span);
            offset += sizeof(int);
        }
        else if (typeof(T) == typeof(long))
        {
            result = (T)(object)BinaryPrimitives.ReadInt64LittleEndian(span);
            offset += sizeof(long);
        }
        else if (typeof(T) == typeof(float))
        {
            result = (T)(object)BitConverter.ToSingle(data, offset);
            offset += sizeof(float);
        }
        else if (typeof(T) == typeof(double))
        {
            result = (T)(object)BitConverter.ToDouble(data, offset);
            offset += sizeof(double);
        }
        else if (typeof(T) == typeof(bool))
        {
            result = (T)(object)(data[offset] != 0);
            offset += sizeof(bool);
        }
        else
        {
            result = default;
        }

        return result;
    }

    public static object ConvertTypeByte2Struct(byte[] data, ref int offset, Type type)
    {
        object result = null;

        int size = Marshal.SizeOf(type);
        byte[] buffer = new byte[size];
        Buffer.BlockCopy(data, offset, buffer, 0, size);
        offset += size;
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(buffer, 0, ptr, size);
            result = Marshal.PtrToStructure(ptr, type);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return result;
    }

    private static ConvertDelegate BuildDelegate(MethodInfo mi)
    {
        var dataParam = Expression.Parameter(typeof(byte[]), "data");
        var offsetParam = Expression.Parameter(typeof(int).MakeByRefType(), "offset");
        // mi(data, ref offset)
        var call = Expression.Call(mi, dataParam, offsetParam);
        // object로 박싱
        var body = Expression.Convert(call, typeof(object));
        return Expression.Lambda<ConvertDelegate>(body, dataParam, offsetParam).Compile();
    }

    private static ConvertDelegate GetConvertDelegate(Type elementType)
    {
        if (_convertDelegateCache.TryGetValue(elementType, out var del))
            return del;

        // 기존 로직으로 MethodInfo 얻기
        MethodInfo mi = typeof(TypeByte2TypeConverter)
            .GetMethod(nameof(Convert), new[] { typeof(byte[]), typeof(int).MakeByRefType() })
            .MakeGenericMethod(elementType);

        // Expression 기반 델리게이트 생성
        del = BuildDelegate(mi);
        _convertDelegateCache[elementType] = del;
        return del;
    }


    public static Array ConvertTypeByte2Array(byte[] data, ref int offset, Type elementType)
    {
        int length = ConvertTypeByte2Primitive<int>(data, ref offset);
        Array array = Array.CreateInstance(elementType, length);

        if (_serializeReference)
        {
            string typeName;
            Type realType;
            for (int i = 0; i < length; i++)
            {
                typeName = ConvertTypeByte2LenString(data, ref offset);
                realType = Type.GetType(typeName);
                var convertMethod = GetConvertDelegate(realType);
                object item = convertMethod(data, ref offset);
                array.SetValue(item, i);
            }
        }
        else
        {
            var convertMethod = GetConvertDelegate(elementType);
            for (int i = 0; i < length; i++)
            {
                object item = convertMethod(data, ref offset);
                array.SetValue(item, i);
            }
        }
        return array;
    }

    public static object ConvertTypeByte2List(byte[] data, ref int offset, Type elementType)
    {
        int count = ConvertTypeByte2Primitive<int>(data, ref offset);

        Type listType = typeof(List<>).MakeGenericType(elementType);
        IList list = (IList)Activator.CreateInstance(listType, count);

        if (_serializeReference)
        {
            string typeName;
            Type realType;
            for (int i = 0; i < count; i++)
            {
                typeName = ConvertTypeByte2LenString(data, ref offset);
                realType = Type.GetType(typeName);
                var convertMethod = GetConvertDelegate(realType);
                object item = convertMethod.Invoke(data, ref offset);
                list.Add(item);
            }
        }
        else
        {
            var convertMethod = GetConvertDelegate(elementType);
            for (int i = 0; i < count; i++)
            {
                object item = convertMethod.Invoke(data, ref offset);
                list.Add(item);
            }
        }
        return list;
    }

    public static object ConvertTypeByte2Dictionary(byte[] data, ref int offset, Type keyType, Type valueType)
    {
        int count = ConvertTypeByte2Primitive<int>(data, ref offset);
        Type dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);

        IDictionary dict = (IDictionary)Activator.CreateInstance(dictType, count);
        var convertKeyMethod = GetConvertDelegate(keyType);
        var convertValueMethod = GetConvertDelegate(valueType);

        for (int i = 0; i < count; i++)
        {
            object key = convertKeyMethod.Invoke(data, ref offset);
            object value = convertValueMethod.Invoke(data, ref offset);

            dict.Add(key, value);
        }
        return dict;
    }

    public static object ConvertTypeByte2HashSet(byte[] data, ref int offset, Type elementType)
    {
        int count = ConvertTypeByte2Primitive<int>(data, ref offset);
        Type setType = typeof(HashSet<>).MakeGenericType(elementType);
        object set = Activator.CreateInstance(setType);
        MethodInfo addMethod = setType.GetMethod("Add");
        var convertMethod = GetConvertDelegate(elementType);

        for (int i = 0; i < count; i++)
        {
            object item = convertMethod(data, ref offset);
            addMethod.Invoke(set, new object[] { item });
        }
        return set;
    }

    private static FieldInfo[] GetSerializableFields(Type type)
    {
        if (!_fieldCache.TryGetValue(type, out var fields))
        {
            const BindingFlags flags = BindingFlags.Instance
                                     | BindingFlags.Public
                                     | BindingFlags.NonPublic
                                     | BindingFlags.DeclaredOnly;

            var list = new List<FieldInfo>();

            // 상속 계층을 순회하며, 각 타입에 직접 선언된 필드를 모은다
            for (var t = type; t != null; t = t.BaseType)
            {
                foreach (var f in t.GetFields(flags))
                {
                    // 1) [NonSerialized] 필드는 무조건 제외
                    if (f.IsNotSerialized)
                        continue;

                    // 2) public 이거나, [SerializeField] 가 붙어 있어야 포함
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

    public static object ConvertTypeByte2Class(byte[] data, ref int offset, Type type)
    {
        object instance = Activator.CreateInstance(type);
        FieldInfo[] fields = GetSerializableFields(type);

        foreach (var field in fields)
        {
            bool isSerializeReference = field.GetCustomAttribute<SerializeReference>() != null;
            bool isNull = !ConvertTypeByte2Primitive<bool>(data, ref offset);
            if (isNull)
            {
                SetFieldValue(instance, field, null);
                continue;
            }

            if (isSerializeReference)
            {
                _serializeReference = true;
                string typeName = ConvertTypeByte2LenString(data, ref offset);
                Type realType = Type.GetType(typeName);
                var convertMethod = GetConvertDelegate(realType);
                object fieldValue = convertMethod(data, ref offset);
                SetFieldValue(instance, field, fieldValue);
                _serializeReference = false;
            }
            else
            {
                bool flag = false;
                if (_serializeReference)
                    flag = true;
                _serializeReference = false;
                Type fieldType = field.FieldType;
                var convertMethod = GetConvertDelegate(fieldType);
                object fieldValue = convertMethod.Invoke(data, ref offset);
                SetFieldValue(instance, field, fieldValue);
                _serializeReference = flag;
            }
        }
        return instance;
    }

    private static void SetFieldValue(object instance, FieldInfo field, object value)
    {
        var key = (field.DeclaringType, field.Name);
        if (!_fieldSetterCache.TryGetValue(key, out var setter))
        {
            
            // Expression을 이용해 필드 setter 델리게이트를 생성 및 캐싱
            var targetParam = Expression.Parameter(typeof(object));
            var valueParam = Expression.Parameter(typeof(object));
            var fieldExp = Expression.Field(Expression.Convert(targetParam, field.DeclaringType), field);
            var assignExp = Expression.Assign(fieldExp, Expression.Convert(valueParam, field.FieldType));
            var lambda = Expression.Lambda<Action<object, object>>(assignExp, targetParam, valueParam);
            setter = lambda.Compile();
            _fieldSetterCache[key] = setter;
        }
        setter(instance, value);
    }

}
