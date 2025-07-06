namespace Microsoft.Vault.Explorer
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing.Design;

    [Editor(typeof(ExpandableCollectionEditor<ObservableLifetimeActionsCollection, LifetimeActionItem>), typeof(UITypeEditor))]
    public class ObservableLifetimeActionsCollection : ObservableCustomCollection<LifetimeActionItem>
    {
        public ObservableLifetimeActionsCollection() : base() { }

        public ObservableLifetimeActionsCollection(IEnumerable<LifetimeActionItem> collection) : base(collection) { }

        protected override PropertyDescriptor GetPropertyDescriptor(LifetimeActionItem item) =>
            new ReadOnlyPropertyDescriptor(item.ToString(), $"DaysBeforeExpiry={Utils.NullableIntToString(item.DaysBeforeExpiry)}, LifetimePercentage={Utils.NullableIntToString(item.LifetimePercentage)}");
    }
}