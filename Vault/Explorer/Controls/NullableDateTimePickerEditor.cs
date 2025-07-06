// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

namespace Microsoft.Vault.Explorer.Controls
{
    using System;
    using System.ComponentModel;
    using System.Drawing.Design;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
    using Microsoft.Vault.Explorer.Common;

    public class NullableDateTimePickerEditor : UITypeEditor
    {
        IWindowsFormsEditorService editorService;
        ToolTip expirationToolTip = new ToolTip();
        DateTimePicker picker = new DateTimePicker();

        public NullableDateTimePickerEditor()
        {
            this.expirationToolTip.ShowAlways = true;
            this.picker.Format = DateTimePickerFormat.Long;
            this.picker.ValueChanged += this.Picker_ValueChanged;
        }

        private void Picker_ValueChanged(object sender, EventArgs e)
        {
            TimeSpan ts = this.picker.Value - DateTime.UtcNow;
            this.expirationToolTip.SetToolTip(this.picker, Utils.ExpirationToString(ts));
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.DropDown;

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                this.editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            }

            if (this.editorService != null)
            {
                if (value != null)
                {
                    this.picker.Value = Convert.ToDateTime(value);
                }
                this.editorService.DropDownControl(this.picker);
                value = new DateTime?(this.picker.Value);
            }

            return value;
        }
    }
}
