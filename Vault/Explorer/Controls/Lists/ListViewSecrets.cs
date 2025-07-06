// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Microsoft.Vault.Explorer
{
    public partial class ListViewSecrets : ListView
    {
        public const int FirstCustomColumnIndex = 4;

        public int SortingColumn { get; set; }

        public ListViewSecrets()
        {
            InitializeComponent();
            ListViewItemSorter = new ListViewSecretsSorter(this);
            DoubleBuffered = true;
            _tags = new Dictionary<string, TagMenuItem>();
        }

        public ListViewItemBase FirstSelectedItem => SelectedItems.Count > 0 ? SelectedItems[0] as ListViewItemBase : null;

        public int SearchResultsCount => Groups[ListViewItemBase.SearchResultsGroup].Items.Count;

        public void RefreshGroupsHeader()
        {
            foreach (var g in Groups.Cast<ListViewGroup>())
            {
                g.Header = $"{g.Name} ({g.Items.Count})";
            }
        }

        private void RemoveTags(IDictionary<string, string> tags)
        {
            if (null == tags) return;
            foreach (string t in tags.Keys)
            {
                if (false == _tags.ContainsKey(t)) continue;
                _tags[t].Count--;
                if (_tags[t].Count < 0)
                {
                    _tags.Remove(t);
                }
            }
        }

        private void AddTags(IDictionary<string, string> tags)
        {
            if (null == tags) return;
            foreach (string t in tags.Keys)
            {
                var tag = _tags.ContainsKey(t) ? _tags[t] : new TagMenuItem(t, this);
                tag.Count++;
                _tags[t] = tag;
            }
        }

        public void AddOrReplaceItem(ListViewItemBase item)
        {
            if (null == item) return;
            if (Items.ContainsKey(item.Name)) // Overwrite flow
            {
                var lvi = Items[item.Name] as ListViewItemBase;
                RemoveTags(lvi.Tags);
                Items.RemoveByKey(item.Name); 
            }
            Items.Add(item);
            AddTags(item.Tags);
        }

        public void RemoveAllItems()
        {
            // Remove custom tag columns
            for (int i = Columns.Count - 1; i >= ListViewSecrets.FirstCustomColumnIndex; i--)
            {
                Columns.RemoveAt(i);
            }
            Items.Clear();
            _tags.Clear();
        }

        public Exception FindItemsWithText(string regexPattern)
        {
            try
            {
                ListViewItemBase selectItem = null;
                BeginUpdate();
                Regex regex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                foreach (ListViewItemBase lvib in Items)
                {
                    bool contains = lvib.Contains(regex);
                    lvib.SearchResult = contains;
                    if ((selectItem == null) && contains)
                    {
                        selectItem = lvib;
                    }
                }
                Sort();
                selectItem?.RefreshAndSelect();
                RefreshGroupsHeader();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
            finally
            {
                EndUpdate();
            }
        }

        public void ToggleSelectedItemsToFromFavorites()
        {
            BeginUpdate();
            foreach (ListViewItemBase lvib in SelectedItems) lvib.Favorite = !lvib.Favorite;
            Sort();
            RefreshGroupsHeader();
            EndUpdate();
        }

        public void ExportToTsv(string filename)
        {
            StringBuilder sb = new StringBuilder();
            // Output column headers
            foreach (ColumnHeader col in Columns)
            {
                sb.AppendFormat("{0}\t", col.Text);
            }
            sb.Append("Status\t");
            sb.Append("Valid from time (UTC)\t");
            sb.Append("Valid until time (UTC)\t");
            sb.Append("Content Type");
            sb.AppendLine();
            // Take all items or in case of multiple selection only the selected ones
            IEnumerable<ListViewItem> items = (SelectedItems.Count <= 1) ? Items.Cast<ListViewItem>() : SelectedItems.Cast<ListViewItem>();
            foreach (ListViewItem item in items)
            {
                sb.AppendLine(item.ToString());
            }
            File.WriteAllText(filename, sb.ToString());
        }

        private void ListViewSecrets_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            BeginUpdate();
            if (SortingColumn == e.Column) // Swap sort order
            {
                Sorting = (Sorting == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                SortingColumn = e.Column;
                Sorting = SortOrder.Ascending;
            }
            EndUpdate();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            // Show uxMenuStripColumns menu in case user right-click on columns header bar
            if ((m.Msg == WM_CONTEXTMENU) && (m.WParam != this.Handle))
            {
                uxMenuStripColumns.Items.Clear();
                var sortedTags = from t in _tags.Keys orderby t select t;
                foreach (string k in sortedTags)
                {
                    uxMenuStripColumns.Items.Add(_tags[k]);
                }
                uxMenuStripColumns.Show(Control.MousePosition);
            }
        }

        private const int WM_CONTEXTMENU = 0x7B;
        private Dictionary<string, TagMenuItem> _tags;
    }
}
