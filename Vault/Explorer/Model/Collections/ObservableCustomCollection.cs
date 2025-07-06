// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.Vault.Explorer
{
    #region ObservableCustomCollection, ExpandableCollectionObjectConverter and ExpandableCollectionEditor
    /// <summary>
    /// Simple wrapper on top of ObservableCollection, so we can enforce some validation logic plus register for:
    /// protected event PropertyChangedEventHandler PropertyChanged;
    /// </summary>
    /// <typeparam name="T">type of the item in the collection</typeparam>
    [TypeConverter(typeof(ExpandableCollectionObjectConverter))]
    public abstract class ObservableCustomCollection<T> : ObservableCollection<T>, ICustomTypeDescriptor where T : class
    {
        private PropertyChangedEventHandler _propertyChanged;
        protected abstract PropertyDescriptor GetPropertyDescriptor(T item);

        public ObservableCustomCollection() : base() { }

        public ObservableCustomCollection(IEnumerable<T> collection) : base(collection) { }

        public void SetPropertyChangedEventHandler(PropertyChangedEventHandler propertyChanged)
        {
            _propertyChanged = propertyChanged;
            PropertyChanged += propertyChanged;
        }

        public PropertyChangedEventHandler GetLastPropertyChangedEventHandler() => _propertyChanged;

        public void AddOrReplace(T item)
        {
            int i = IndexOf(item);
            if (i == -1) Add(item); else SetItem(i, item);
        }

        public void AddOrKeep(T item)
        {
            int i = IndexOf(item);
            if (i == -1) Add(item);
        }

        public T GetOrNull(T item)
        {
            int i = IndexOf(item);
            return (i == -1) ? null : this[i];
        }

        #region ICustomTypeDescriptor interface to show properties in PropertyGrid

        public string GetComponentName() => TypeDescriptor.GetComponentName(this, true);

        public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);

        public string GetClassName() => TypeDescriptor.GetClassName(this, true);

        public EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);

        public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);

        public TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);

        public object GetPropertyOwner(PropertyDescriptor pd) => this;

        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);

        public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);

        public PropertyDescriptor GetDefaultProperty() => null;

        public PropertyDescriptorCollection GetProperties() => GetProperties(new Attribute[0]);

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return new PropertyDescriptorCollection((from item in this select GetPropertyDescriptor(item)).ToArray());
        }

        #endregion
    }

    #endregion

    #region TagItems

    #endregion

    #region LifetimeActionItems

    #endregion
}
