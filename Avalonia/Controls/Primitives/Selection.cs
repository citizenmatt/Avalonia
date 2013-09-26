﻿// -----------------------------------------------------------------------
// <copyright file="Selection.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Avalonia.Controls.Primitives
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Avalonia.Data;
    using Avalonia.Input;

    internal class Selection
    {
        static readonly object[] Empty = new object[0];

        public Selection(Selector owner)
        {
            this.Owner = owner;
            this.Owner.SelectedItems.CollectionChanged += HandleOwnerSelectionChanged;
            SelectedItems = new List<object>();
        }

        public SelectionMode Mode { get; set; }

        object SelectedItem { get; set; }

        List<object> SelectedItems { get; set; }

        public bool Updating { get; set; }

        private Selector Owner { get; set; }

        private void HandleOwnerSelectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // When 'Updating' is false it means the user has directly modified the collection
            // by calling ListBox.SelectedItems.[Add|Remove]. In this case we need to ensure we
            // don't have a duplicate selection.
            if (!Updating)
            {
                if (Mode == SelectionMode.Single)
                    throw new InvalidOperationException("SelectedItems cannot be modified directly when in Single select mode");
                try
                {
                    Updating = true;
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            if (!SelectedItems.Contains(e.NewItems[0]))
                                AddToSelected(e.NewItems[0]);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            if (SelectedItems.Contains(e.OldItems[0]))
                                RemoveFromSelected(e.OldItems[0]);
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            if (SelectedItems.Contains(e.OldItems[0]))
                                RemoveFromSelected(e.OldItems[0]);
                            if (!SelectedItems.Contains(e.NewItems[0]))
                                AddToSelected(e.NewItems[0]);
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            foreach (var v in SelectedItems.Where(o => !Owner.SelectedItems.Contains(o)).ToArray())
                                if (SelectedItems.Contains(v))
                                    RemoveFromSelected(v);
                            foreach (var v in Owner.SelectedItems.Where(o => !SelectedItems.Contains(o)).ToArray())
                                if (!SelectedItems.Contains(v))
                                    AddToSelected(v);
                            break;
                    }

                    Owner.SelectedItemsIsInvalid = true;
                }
                finally
                {
                    Updating = false;
                }
            }
        }

        internal void RepopulateSelectedItems()
        {
            if (!Updating)
            {
                try
                {
                    Updating = true;
                    Owner.SelectedItems.Clear();
                    Owner.SelectedItems.AddRange(SelectedItems);
                }
                finally
                {
                    Updating = false;
                }
            }
        }

        public void ClearSelection()
        {
            ClearSelection(false);
        }

        public void ClearSelection(bool ignoreSelectedValue)
        {
            if (SelectedItems.Count == 0)
            {
                UpdateSelectorProperties(null, -1, ignoreSelectedValue ? Owner.SelectedValue : null);
                return;
            }

            try
            {
                Updating = true;
                var oldSelection = SelectedItems.Cast<object>().ToArray();

                SelectedItems.Clear();
                SelectedItem = null;
                UpdateSelectorProperties(null, -1, ignoreSelectedValue ? Owner.SelectedValue : null);

                Owner.SelectedItemsIsInvalid = true;
                Owner.RaiseSelectionChanged(oldSelection, Empty);
            }
            finally
            {
                Updating = false;
            }
        }

        public void Select(object item)
        {
            Select(item, false);
        }

        public void Select(object item, bool ignoreSelectedValue)
        {
            // Ignore any Select requests for items which aren't in the  owners Items list
            if (!Owner.Items.Contains(item))
                return;

            bool selected = SelectedItems.Contains(item);

            try
            {
                Updating = true;

                switch (Mode)
                {
                    case SelectionMode.Single:
                        // When in single select mode we unselect the item if the Control key is held,
                        // otherwise we just ensure that the SelectedIndex is in sync. It could be out
                        // of sync if the user inserts an item before the current selected item.
                        if (selected)
                        {
                            ////if (ModifierKeys.Control == (Keyboard.Modifiers & ModifierKeys.Control))
                            ////    ClearSelection(ignoreSelectedValue);
                            ////else
                            ////    UpdateSelectorProperties(SelectedItem, Owner.Items.IndexOf(SelectedItem), Owner.SelectedValue);
                        }
                        else
                        {
                            ReplaceSelection(item);
                        }
                    
                        break;
                    
                    case SelectionMode.Extended:
                        ////if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        ////{
                        ////    int selectedIndex = Owner.Items.IndexOf(SelectedItem);
                        ////    if (SelectedItems.Count == 0)
                        ////        SelectRange(0, Owner.Items.IndexOf(item));
                        ////    else
                        ////        SelectRange(selectedIndex, Owner.Items.IndexOf(item));
                        ////}
                        ////else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        ////{
                        ////    if (!selected)
                        ////        AddToSelected(item);
                        ////}
                        ////else
                        ////{
                        ////    if (selected)
                        ////        RemoveFromSelected(item);
                        ////    else
                        ////        AddToSelected(item);
                        ////}
                        break;

                    case SelectionMode.Multiple:
                        if (SelectedItems.Contains(item))
                        {
                            UpdateSelectorProperties(SelectedItem, Owner.Items.IndexOf(SelectedItem), Owner.SelectedValue);
                        }
                        else
                        {
                            AddToSelected(item);
                        }
                        
                        break;

                    default:
                        throw new NotSupportedException(string.Format("SelectionMode {0} is not support", Mode));
                }
            }
            finally
            {
                Updating = false;
            }
        }

        public void SelectRange(int startIndex, int endIndex)
        {
            // First get all the objects which should be selected
            List<object> toSelect = new List<object>();
            for (int i = startIndex; i <= endIndex; i++)
                toSelect.Add(Owner.Items[i]);

            // Then get all the existing selected items which need to be unselected
            List<object> toUnselect = new List<object>();
            foreach (object o in SelectedItems)
                if (!toSelect.Contains(o))
                    toUnselect.Add(o);

            // Then remove items from 'toSelect' which are already selected
            foreach (object o in SelectedItems)
                if (toSelect.Contains(o))
                    toSelect.Remove(o);

            // Now we have the diff between what needs to be selected and unselected
            // so make the changes
            foreach (object o in toUnselect)
                SelectedItems.Remove(o);

            foreach (object o in toSelect)
                SelectedItems.Add(o);

            if (!SelectedItems.Contains(SelectedItem))
            {
                SelectedItem = SelectedItems.Count == 0 ? null : SelectedItems[0];
                UpdateSelectorProperties(SelectedItem, SelectedItem == null ? -1 : Owner.Items.IndexOf(SelectedItem), Owner.GetValueFromItem(SelectedItem));
            }

            Owner.SelectedItemsIsInvalid = true;
            Owner.RaiseSelectionChanged(toUnselect, toSelect);
        }

        public void SelectAll(ItemCollection items)
        {
            try
            {
                Updating = true;

                if (Mode == SelectionMode.Single)
                    throw new NotSupportedException("Cannot call SelectAll when in Single select mode");

                var toSelect = new List<object>();
                foreach (var v in items)
                    if (!SelectedItems.Contains(v))
                        toSelect.Add(v);

                if (toSelect.Count == 0)
                    return;

                SelectedItems.AddRange(toSelect);
                if (SelectedItem == null)
                {
                    SelectedItem = toSelect[0];
                    UpdateSelectorProperties(SelectedItem, Owner.Items.IndexOf(SelectedItem), Owner.GetValueFromItem(SelectedItem));
                }

                Owner.SelectedItemsIsInvalid = true;
                Owner.RaiseSelectionChanged(Empty, toSelect);
            }
            finally
            {
                Updating = false;
            }
        }

        public void SelectOnly(object item)
        {
            if (SelectedItem == item && SelectedItems.Count == 1)
                return;

            try
            {
                Updating = true;
                ReplaceSelection(item);
            }
            finally
            {
                Updating = false;
            }
        }

        public void Unselect(object item)
        {
            if (!SelectedItems.Contains(item))
                return;

            try
            {
                Updating = true;
                RemoveFromSelected(item);
            }
            finally
            {
                Updating = false;
            }
        }

        void AddToSelected(object item)
        {
            SelectedItems.Add(item);
            if (SelectedItems.Count == 1)
            {
                SelectedItem = item;
                UpdateSelectorProperties(item, Owner.Items.IndexOf(item), Owner.GetValueFromItem(item));
            }

            Owner.SelectedItemsIsInvalid = true;
            Owner.RaiseSelectionChanged(Empty, new object[] { item });
        }

        void RemoveFromSelected(object item)
        {
            SelectedItems.Remove(item);
            if (SelectedItem == item)
            {
                var newItem = SelectedItems.Count == 0 ? null : SelectedItems[0];
                SelectedItem = newItem;
                UpdateSelectorProperties(newItem, newItem == null ? -1 : Owner.Items.IndexOf(newItem), Owner.GetValueFromItem(item));
            }

            Owner.SelectedItemsIsInvalid = true;
            Owner.RaiseSelectionChanged(new object[] { item }, Empty);
        }

        void ReplaceSelection(object item)
        {
            if (!UpdateCollectionView(item))
            {
                UpdateSelectorProperties(SelectedItem, Owner.Items.IndexOf(SelectedItem), Owner.GetValueFromItem(SelectedItem));
                return;
            }

            var addedItems = Empty;
            var oldItems = SelectedItems.Cast<object>().Where(o => o != item).ToArray();

            // Unselect all the previously selected items
            foreach (var v in oldItems)
                SelectedItems.Remove(v);

            // If we previously had the current item selected, it will be the only one the list now
            // so we only have to add it if the list is empty.
            if (SelectedItems.Count == 0)
            {
                addedItems = new object[] { item };
                SelectedItems.Add(item);
            }

            // Always update the selection properties to keep everything nicely in sync. These could get out of sync
            // if (for example) the user inserts an item at the start of the ItemsControl.Items collection.
            SelectedItem = item;
            UpdateSelectorProperties(item, Owner.Items.IndexOf(item), Owner.GetValueFromItem(item));

            if (addedItems != Empty || oldItems != Empty)
            {
                // Refresh the Selector.SelectedItems list
                Owner.SelectedItemsIsInvalid = true;

                // Raise our SelectionChanged event
                Owner.RaiseSelectionChanged(oldItems, addedItems);
            }
        }

        void UpdateSelectorProperties(object item, int index, object value)
        {
            if (Owner.SelectedItem != item)
                Owner.SelectedItem = item;

            if (Owner.SelectedIndex != index)
                Owner.SelectedIndex = index;

            if (Owner.SelectedValue != value)
                Owner.SelectedValue = value;

            UpdateCollectionView(item);
        }

        bool UpdateCollectionView(object item)
        {
            if (Owner.ItemsSource is ICollectionView)
            {
                var icv = (ICollectionView)Owner.ItemsSource;
                icv.MoveCurrentTo(item);
                return object.Equals(item, icv.CurrentItem);
            }
            return true;
        }
    }

}
