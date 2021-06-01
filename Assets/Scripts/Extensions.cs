using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public static class Extensions
{
    public static List<T> Splice<T>(this List<T> source, int index, int count)
    {
        var items = source.GetRange(index, count);
        source.RemoveRange(index, count);
        return items;
    }

    public static long GetCurrentTime()
    {
        DateTime baseDate = new DateTime(1970, 1, 1);
        TimeSpan diff = DateTime.Now - baseDate;
        return (long)diff.TotalMilliseconds;
    }
}
