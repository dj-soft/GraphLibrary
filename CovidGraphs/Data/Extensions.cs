﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DW = System.Drawing;
using WF = System.Windows.Forms;

namespace Djs.Tools.CovidGraphs.Data
{
    public static class Extensions
    {
        public static int GetDateKey(this DateTime date)
        {
            return 10000 * date.Year + 100 * date.Month + date.Day;
        }
        public static int GetDateKeyShort(this DateTime date)
        {
            TimeSpan time = date - new DateTime(2019, 1, 1);
            return (int)time.TotalDays;
        }
        public static TValue AddOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> data, TKey key, Func<TValue> creator)
        {
            if (data.TryGetValue(key, out TValue value)) return value;
            value = creator();
            data.Add(key, value);
            return value;
        }
        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> data, TKey key, TValue value)
        {
            if (!data.ContainsKey(key))
                data.Add(key, value);
            else
                data[key] = value;
        }
        public static void AddIfNotContains<TKey, TValue>(this Dictionary<TKey, TValue> data, TKey key, TValue value)
        {
            if (!data.ContainsKey(key))
                data.Add(key, value);
        }
        public static TValue[] GetSortedValues<TKey, TValue>(this Dictionary<TKey, TValue> data, Func<TValue, TValue, int> sorter)
        {
            List<TValue> values = data.Values.ToList();
            values.Sort((a, b) => sorter(a, b));
            return values.ToArray();
        }
        public static void ForEachExec<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
                action(item);
        }
        public static int[] GetFormPosition(this WF.Form form)
        {
            int[] position = new int[5];
            bool isMaximized = (form.WindowState == WF.FormWindowState.Maximized);
            DW.Rectangle bounds = (isMaximized ? form.RestoreBounds : form.Bounds);      // Pokud je nyní Form maximalizovaný, pak uložím souřadnice v Normal stavu (=ne současné maximalizované)
            position[0] = (isMaximized ? 2 : 1);
            position[1] = bounds.X;
            position[2] = bounds.Y;
            position[3] = bounds.Width;
            position[4] = bounds.Height;
            return position;
        }
        public static void SetFormPosition(this WF.Form form, int[] position)
        {
            if (position == null || position.Length < 5) return;
            form.Bounds = new DW.Rectangle(position[1], position[2], position[3], position[4]);   // Toto jsou souřadnice normální, nikoli Maximized
            form.WindowState = (position[0] == 2 ? WF.FormWindowState.Maximized : WF.FormWindowState.Normal);
            form.StartPosition = WF.FormStartPosition.Manual;
        }
        public static DW.Rectangle AlignTo(this DW.Rectangle bounds, DW.Rectangle parentBounds)
        {
            int px = parentBounds.X;
            int py = parentBounds.Y;
            int pw = parentBounds.Width;
            int ph = parentBounds.Height;

            int bx = bounds.X;
            int by = bounds.Y;
            int bw = bounds.Width;
            int bh = bounds.Height;

            AlignTo1D(ref bx, ref bw, px, pw);
            AlignTo1D(ref by, ref bh, py, ph);

            return new DW.Rectangle(bx, by, bw, bh);
        }
        private static void AlignTo1D(ref int b0, ref int bs, int c0, int cs)
        {
            if (b0 < c0) b0 = c0;
            int c1 = c0 + cs;
            if ((b0 + bs) > c1)
            {
                b0 = c1 - bs;
                if (b0 < c0)
                {
                    b0 = c0;
                    bs = cs;
                }
            }
        }
    }
}
