﻿namespace Microsoft.Vault.Explorer.Controls
{
    using System.Reflection;
    using System.Windows.Forms;

    internal static class PropertyGridSplitter
    {
        public static void SetLabelColumnWidth(this PropertyGrid grid, int width)
        {
            FieldInfo fi = grid?.GetType().GetField("_gridView", BindingFlags.Instance | BindingFlags.NonPublic);
            Control view = fi?.GetValue(grid) as Control;
            MethodInfo mi = view?.GetType().GetMethod("MoveSplitterTo", BindingFlags.Instance | BindingFlags.NonPublic);
            mi?.Invoke(view, new object[] { width });
        }
    }
}