using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;

public static class ExtensionMethods 
{
    //-------------------------------------------------------------------------//
    // List Shuffling
    //-------------------------------------------------------------------------//

    public static void Shuffle<T>(this IList<T> list) {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static T RandomItem<T>(this IList<T> list) {
        if (list.Count == 0) throw new System.IndexOutOfRangeException("Cannot select a random item from an empty list");
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static T RemoveRandom<T>(this IList<T> list) {
        if (list.Count == 0) throw new System.IndexOutOfRangeException("Cannot remove a random item from an empty list");
        int index = UnityEngine.Random.Range(0, list.Count);
        T item = list[index];
        list.RemoveAt(index);
        return item;
    }

    //-------------------------------------------------------------------------//
    // String Methods
    //-------------------------------------------------------------------------//

    public static string Truncate(this string value, int maxLength) {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    public static string ToString(this object anObject, string aFormat) {
        return ToString(anObject, aFormat, null);
    }

    public static string ToString(this object anObject, string aFormat, IFormatProvider formatProvider) {
        StringBuilder sb = new StringBuilder();
        Type type = anObject.GetType();
        Regex reg = new Regex(@"({)([^}]+)(})", RegexOptions.IgnoreCase);
        MatchCollection mc = reg.Matches(aFormat);
        int startIndex = 0;
        foreach (Match m in mc) {
            Group g = m.Groups[2]; //it's second in the match between { and }
            int length = g.Index - startIndex - 1;
            sb.Append(aFormat.Substring(startIndex, length));

            string toGet = string.Empty;
            string toFormat = string.Empty;
            int formatIndex = g.Value.IndexOf(":"); //formatting would be to the right of a :
            if (formatIndex == -1) //no formatting, no worries
            {
                toGet = g.Value;
            }
            else //pickup the formatting
            {
                toGet = g.Value.Substring(0, formatIndex);
                toFormat = g.Value.Substring(formatIndex + 1);
            }

            //first try properties
            PropertyInfo retrievedProperty = type.GetProperty(toGet);
            Type retrievedType = null;
            object retrievedObject = null;
            if (retrievedProperty != null) {
                retrievedType = retrievedProperty.PropertyType;
                retrievedObject = retrievedProperty.GetValue(anObject, null);
            }
            else //try fields
            {
                FieldInfo retrievedField = type.GetField(toGet);
                if (retrievedField != null) {
                    retrievedType = retrievedField.FieldType;
                    retrievedObject = retrievedField.GetValue(anObject);
                }
            }

            if (retrievedType != null) //Cool, we found something
            {
                string result = string.Empty;
                if (toFormat == string.Empty) //no format info
                {
                    result = retrievedType.InvokeMember("ToString",
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase
                        , null, retrievedObject, null) as string;
                }
                else //format info
                {
                    result = retrievedType.InvokeMember("ToString",
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase
                        , null, retrievedObject, new object[] { toFormat, formatProvider }) as string;
                }
                sb.Append(result);
            }
            else //didn't find a property with that name, so be gracious and put it back
            {
                sb.Append("{");
                sb.Append(g.Value);
                sb.Append("}");
            }
            startIndex = g.Index + g.Length + 1;
        }
        if (startIndex < aFormat.Length) //include the rest (end) of the string
        {
            sb.Append(aFormat.Substring(startIndex));
        }
        return sb.ToString();
    }

    //-------------------------------------------------------------------------//
    // Vector Methods
    //-------------------------------------------------------------------------//

    public static Vector2 xy(this Vector3 v) {
        return new Vector2(v.x, v.y);
    }

    public static Vector2 xz(this Vector3 v) {
        return new Vector2(v.x, v.z);
    }

    public static Vector2 yz(this Vector3 v) {
        return new Vector2(v.y, v.z);
    }

    public static Vector3 WithX(this Vector3 v, float x) {
        return new Vector3(x, v.y, v.z);
    }

    public static Vector3 WithY(this Vector3 v, float y) {
        return new Vector3(v.x, y, v.z);
    }

    public static Vector3 WithZ(this Vector3 v, float z) {
        return new Vector3(v.x, v.y, z);
    }

    public static Vector2 WithX(this Vector2 v, float x) {
        return new Vector2(x, v.y);
    }

    public static Vector2 WithY(this Vector2 v, float y) {
        return new Vector2(v.x, y);
    }

    public static Vector3 WithZ(this Vector2 v, float z) {
        return new Vector3(v.x, v.y, z);
    }

    public static Vector3 NearestPointOnAxis(this Vector3 axisDirection, Vector3 point, bool isNormalized = false) {
        if (!isNormalized) axisDirection.Normalize();
        var d = Vector3.Dot(point, axisDirection);
        return axisDirection * d;
    }

    public static Vector3 NearestPointOnLine(this Vector3 lineDirection, Vector3 point, Vector3 pointOnLine, bool isNormalized = false) {
        if (!isNormalized) lineDirection.Normalize();
        var d = Vector3.Dot(point - pointOnLine, lineDirection);
        return pointOnLine + (lineDirection * d);
    }
}
