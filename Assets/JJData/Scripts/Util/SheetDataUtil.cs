using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

public static class SheetDataUtil
{
    private static Dictionary<string, object> _cach;

    static SheetDataUtil()
    {
        _cach = new();
    }

    /// <summary>
    /// Sheet 데이터를 특정 행을 key값으로 Dictionary 형태로 맵핑해준다.
    /// 재 호출시 Cach 된 값을 반환하긴 하지만 과정에 언박싱이 있으니 따로 저장해서 사용해주자.
    /// </summary>
    /// <typeparam name="RowType">SheetData Type</typeparam>
    /// <typeparam name="KeyType">Key Type</typeparam>
    /// <param name="source">SheetData</param>
    /// <param name="keyColumn">KeyColumn 변수명</param>
    /// <param name="jenericRef">Jeneric 명시가 귀찮을때 제네릭 참고용 (맵핑용 딕셔너리를 넣어주자)</param>
    /// <returns></returns>
    public static Dictionary<KeyType, RowType> DicByKey<RowType, KeyType>(RowType[] source, string keyColumn, Dictionary<KeyType, RowType> jenericRef)
    {
        string cachKey = $"{typeof(RowType).FullName}.{typeof(KeyType).Name}.{keyColumn}";
        if (_cach.ContainsKey(cachKey))
            return (Dictionary<KeyType, RowType>)_cach[cachKey];

        Dictionary<KeyType, RowType> result = new();

        var field = typeof(RowType).GetField(keyColumn, BindingFlags.Instance | BindingFlags.Public);

        for (int i = 0; i < source.Length; i++)
        {
            result[(KeyType)field.GetValue(source[i])] = source[i];
        }

        _cach[cachKey] = result;
        return result;
    }

    /// <summary>
    /// Sheet 데이터를 특정 행을 key값으로 Dictionary 형태로 맵핑해준다.
    /// 재 호출시 Cach 된 값을 반환하긴 하지만 과정에 언박싱이 있으니 따로 저장해서 사용해주자.
    /// </summary>
    /// <typeparam name="RowType">SheetData Type</typeparam>
    /// <typeparam name="KeyType">Key Type</typeparam>
    /// <param name="source">SheetData</param>
    /// <param name="keyColumn">KeyColumn 변수명</param>
    /// <returns></returns>
    public static Dictionary<KeyType, RowType> DicByKey<RowType, KeyType>(RowType[] source, string keyColumn)
    {
        string cachKey = $"{typeof(RowType).FullName}.{typeof(KeyType).Name}.{keyColumn}";
        if (_cach.ContainsKey(cachKey))
            return (Dictionary<KeyType, RowType>)_cach[cachKey];

        Dictionary<KeyType, RowType> result = new();

        var field = typeof(RowType).GetField(keyColumn, BindingFlags.Instance | BindingFlags.Public);

        for (int i = 0; i < source.Length; i++)
        {
            result[(KeyType)field.GetValue(source[i])] = source[i];
        }

        _cach[cachKey] = result;
        return result;
    }

    /// <summary>
    /// Sheet 데이터를 특정 행을 key값으로 Dictionary 형태로 맵핑해준다.
    /// 재 호출시 Cach 된 값을 반환하긴 하지만 과정에 언박싱이 있으니 따로 저장해서 사용해주자.
    /// </summary>
    /// <typeparam name="RowType">SheetData Type</typeparam>
    /// <typeparam name="KeyType">Key Type</typeparam>
    /// <param name="source">SheetData</param>
    /// <param name="keySelector">x => x.{변수}  형태로 전달.</param>
    public static Dictionary<KeyType, RowType> DicByKey<RowType, KeyType>(RowType[] source, Expression<Func<RowType, KeyType>> keySelector)
    {
        MemberExpression memberExpr = (MemberExpression)keySelector.Body;
        string keyColumn = memberExpr.Member.Name;
        string cachKey = $"{typeof(RowType).FullName}.{typeof(KeyType).Name}.{keyColumn}";

        if (_cach.ContainsKey(cachKey))
            return (Dictionary<KeyType, RowType>)_cach[cachKey];

        var getter = keySelector.Compile();
        Dictionary<KeyType, RowType> result = new();

        for (int i = 0; i < source.Length; i++)
            result[getter(source[i])] = source[i];

        _cach[cachKey] = result;
        return result;
    }
}
