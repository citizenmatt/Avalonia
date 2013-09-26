// -----------------------------------------------------------------------
// <copyright file="Selector.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Avalonia.Controls.Primitives
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using Avalonia.Data;

    public abstract partial class Selector : ItemsControl, ISupportInitialize
    {
        internal const string TemplateScrollViewerName = "ScrollViewer";

        public static readonly DependencyProperty IsSynchronizedWithCurrentItemProperty =
            DependencyProperty.Register(
                "IsSynchronizedWithCurrentItem", 
                typeof(bool?), 
                typeof(Selector),
                new PropertyMetadata(
                    null, 
                    OnIsSynchronizedWithCurrentItemChanged));

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(
                "SelectedIndex", 
                typeof(int), 
                typeof(Selector),
                new FrameworkPropertyMetadata(
                    -1, 
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                    OnSelectedIndexChanged));

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(
                "SelectedValue", 
                typeof(object), 
                typeof(Selector), 
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                    OnSelectedValueChanged));

        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register(
                "SelectedValuePath", 
                typeof(string), 
                typeof(Selector), 
                new PropertyMetadata(
                    string.Empty, 
                    OnSelectedValuePathChanged));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                "SelectedItem", 
                typeof(object), 
                typeof(Selector),
                new PropertyMetadata(OnSelectedItemChanged_cb));

        internal static readonly DependencyProperty IsSelectionActiveProperty =
            DependencyProperty.Register(
                "IsSelectionActive",
                typeof(bool),
                typeof(Selector),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.Inherits));

        private ObservableCollection<object> selectedItems;

        internal Selector()
        {
            this.selectedItems = new ObservableCollection<object>();
            this.Selection = new Selection(this);
        }

        public event SelectionChangedEventHandler SelectionChanged;

        ////[TypeConverter(typeof(NullableBoolConverter))]
        public bool? IsSynchronizedWithCurrentItem
        {
            get { return (bool?)GetValue(IsSynchronizedWithCurrentItemProperty); }
            set { SetValue(IsSynchronizedWithCurrentItemProperty, value); }
        }

        public int SelectedIndex
        {
            get { return (int)this.GetValue(SelectedIndexProperty); }
            set { this.SetValue(SelectedIndexProperty, value); }
        }

        public object SelectedItem
        {
            get { return this.GetValue(SelectedItemProperty); }
            set { this.SetValue(SelectedItemProperty, value); }
        }

        public object SelectedValue
        {
            get { return this.GetValue(SelectedValueProperty); }
            set { this.SetValue(SelectedValueProperty, value); }
        }

        public string SelectedValuePath
        {
            get { return (string)this.GetValue(SelectedValuePathProperty); }
            set { this.SetValue(SelectedValuePathProperty, value); }
        }

        internal bool IsSelectionActive
        {
            get { return (bool)this.GetValue(IsSelectionActiveProperty); }
            set { this.SetValue(IsSelectionActiveProperty, value); }
        }

        internal bool SelectedItemsIsInvalid
        {
            get;
            set;
        }

        internal ObservableCollection<object> SelectedItems
        {
            get
            {
                if (SelectedItemsIsInvalid)
                {
                    Selection.RepopulateSelectedItems();
                }
                return selectedItems;
            }
        }

        internal PropertyPathWalker SelectedValueWalker
        {
            get;
            set;
        }

        internal Selection Selection
        {
            get;
            private set;
        }

        internal ScrollViewer TemplateScrollViewer
        {
            get;
            private set;
        }

        private bool Initializing
        {
            get;
            set;
        }

        private State InitState
        {
            get;
            set;
        }

        private bool SynchronizeWithCurrentItem
        {
            get
            {
                bool? sync = IsSynchronizedWithCurrentItem;

                return (ItemsSource is ICollectionView) && (!sync.HasValue || sync.Value);
            }
        }

        public static bool GetIsSelectionActive(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(ListBox.IsSelectionActiveProperty);
        }

        internal object GetValueFromItem(object item)
        {
            if (SelectedValueWalker == null)
                return item;
            else if (item == null)
                return item;
            else
                return SelectedValueWalker.GetValue(item);
        }

        internal static void ItemContainerStyleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((Selector)sender).OnItemContainerStyleChanged((Style)e.OldValue, (Style)e.NewValue);
        }

        internal virtual void OnItemContainerStyleChanged(Style oldStyle, Style newStyle)
        {
        }

        internal override void OnItemsSourceChanged(IEnumerable oldSource, IEnumerable newSource)
        {
            base.OnItemsSourceChanged(oldSource, newSource);

            ICollectionView view = oldSource as ICollectionView;
            
            if (view != null)
            {
                view.CurrentChanged -= OnCurrentItemChanged;
            }

            view = newSource as ICollectionView;
            
            if (view != null)
            {
                view.CurrentChanged += OnCurrentItemChanged;

                if (SynchronizeWithCurrentItem)
                {
                    Selection.SelectOnly(view.CurrentItem);
                }
                else
                {
                    Selection.ClearSelection();
                }
            }
            else
            {
                Selection.ClearSelection();
            }
        }

        internal void RaiseSelectionChanged(IList oldVals, IList newVals)
        {
            oldVals = oldVals ?? new object[0];
            newVals = newVals ?? new object[0];

            foreach (var oldValue in oldVals)
            {
                if (oldValue != null)
                {
                    var oldItem = (ListBoxItem)((oldValue as ListBoxItem) ?? ItemContainerGenerator.ContainerFromItem(oldValue));

                    if (oldItem != null)
                        oldItem.IsSelected = false;
                }
            }

            foreach (var newValue in newVals)
            {
                if (newValue != null)
                {
                    var newItem = (ListBoxItem)((newValue as ListBoxItem) ?? ItemContainerGenerator.ContainerFromItem(newValue));

                    if (newItem != null)
                    {
                        newItem.IsSelected = true;
                        // FIXME: Sometimes the item should be focused and sometimes it shouldn't
                        // I think that the selector won't steal focus from an element which isn't
                        // a child of the selector.
                        // Testcase:
                        // 1) Open the Controls Toolkit.
                        // 2) Click on a demo in the treeview
                        // 3) Try to shrink the source textbox view.
                        // Result: The view requires 2 clicks to collapse it. Subsequent attempts work on the first click.
                        // This 'bug' should only happen if you change the source view tab manually, i.e. if you change the
                        // source file being displayed you will need two clicks to collapse the view.
                        ////newItem.Focus();
                    }
                }
            }

            if (this.SelectionChanged != null)
            {
                this.SelectionChanged(this, new SelectionChangedEventArgs(oldVals, newVals));
            }
        }

        private void IsSynchronizedWithCurrentItemChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            bool? sync = (bool?)e.NewValue;

            if (sync.HasValue && sync.Value)
                throw new ArgumentException("Setting IsSynchronizedWithCurrentItem to 'true' is not supported");

            if (!sync.HasValue && ItemsSource is ICollectionView)
            {
                SelectedItem = ((ICollectionView)ItemsSource).CurrentItem;
            }
            else
            {
                SelectedItem = null;
            }
        }

        private void OnCurrentItemChanged(object sender, EventArgs args)
        {
            if (!Selection.Updating && SynchronizeWithCurrentItem)
            {
                var icv = (ICollectionView)ItemsSource;

                if (!object.Equals(icv.CurrentItem, SelectedItem))
                {
                    Selection.SelectOnly(icv.CurrentItem);
                }
            }
        }

        private void SelectedIndexChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (Selection.Updating || Initializing)
                return;

            var newVal = (int)e.NewValue;
            if (newVal < 0 || newVal >= Items.Count)
            {
                Selection.ClearSelection();
            }
            else
            {
                Selection.Select(Items[newVal]);
            }
        }

        private void SelectedItemChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (Selection.Updating || Initializing)
            {
                return;
            }

            // If the new item is null we clear our selection. If it is non-null
            // and not in the Items array, then we revert to the old selection as
            // we can't select something which is not in the Selector.
            if (e.NewValue == null)
            {
                Selection.ClearSelection();
            }
            else if (Items.IndexOf(e.NewValue) != -1)
            {
                Selection.Select(e.NewValue);
            }
            else if (Items.IndexOf(e.OldValue) != -1)
            {
                Selection.Select(e.OldValue);
            }
            else
            {
                Selection.ClearSelection();
            }
        }

        private void SelectedValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (Selection.Updating || Initializing)
            {
                return;
            }

            SelectItemFromValue(e.NewValue, false);
        }

        private void SelectItemFromValue(object selectedValue, bool ignoreSelectedValue)
        {
            if (selectedValue == null)
            {
                Selection.ClearSelection(ignoreSelectedValue);
                return;
            }

            foreach (var item in Items)
            {
                var value = GetValueFromItem(item);

                if (object.Equals(selectedValue, value))
                {
                    // FIXME: I don't like this check here, but it fixes drt 232. What was happening
                    // is that if we set the selected value to the same thing twice we'd end up
                    // unselecting the item instead of maintaining the selection.
                    if (!SelectedItems.Contains(item))
                    {
                        Selection.Select(item, ignoreSelectedValue);
                    }

                    return;
                }
            }

            Selection.ClearSelection(ignoreSelectedValue);
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);
            ListBoxItem lbItem = (ListBoxItem)element;
            
            lbItem.ParentSelector = null;
            
            if (element != item)
            {
                lbItem.Content = null;
            }
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            ListBoxItem listBoxItem = (ListBoxItem)element;
            listBoxItem.ParentSelector = this;

            if (SelectedItems.Contains(item))
            {
                listBoxItem.IsSelected = true;
            }

            if (listBoxItem.IsSelected && !SelectedItems.Contains(item))
            {
                Selection.Select(item);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            ////TemplateScrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;

            ////if (TemplateScrollViewer != null)
            ////{
            ////    TemplateScrollViewer.TemplatedParentHandlesScrolling = true;
            ////    // Update ScrollViewer values
            ////    TemplateScrollViewer.HorizontalScrollBarVisibility = ScrollViewer.GetHorizontalScrollBarVisibility(this);
            ////    TemplateScrollViewer.VerticalScrollBarVisibility = ScrollViewer.GetVerticalScrollBarVisibility(this);
            ////}
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            if (Initializing)
            {
                base.OnItemsChanged(e);
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    ListBoxItem item = e.NewItems[0] as ListBoxItem;

                    if (item != null && item.IsSelected && !SelectedItems.Contains(item))
                    {
                        Selection.Select(item);
                    }
                    else if (SelectedItem != null)
                    {
                        // The index of our selected item may have changed, so we need to
                        // reselect it to refresh the SelectedIndex property. This won't raise
                        // a SelectionChanged event as the actual object is the same.
                        Selection.Select(SelectedItem);
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    object o;

                    if (ItemsSource is ICollectionView && SynchronizeWithCurrentItem)
                    {
                        o = ((ICollectionView)ItemsSource).CurrentItem;
                    }
                    else
                    {
                        o = SelectedItem;
                    }

                    if (Items.Contains(o))
                    {
                        Selection.Select(o);
                    }
                    else
                    {
                        Selection.ClearSelection();
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (SelectedItems.Contains(e.OldItems[0]))
                    {
                        Selection.Unselect(e.OldItems[0]);
                    }
                    else if (e.OldStartingIndex <= SelectedIndex)
                    {
                        Selection.Select(SelectedItem);
                    }

                    break;
                
                case NotifyCollectionChangedAction.Replace:
                    Selection.Unselect(e.OldItems[0]);
                    break;
                
                default:
                    throw new NotSupportedException(string.Format("Collection changed action '{0}' not supported", e.Action));
            }
            
            base.OnItemsChanged(e);
        }

        internal virtual void NotifyListItemClicked(ListBoxItem listBoxItem)
        {
            Selection.Select(ItemContainerGenerator.ItemFromContainer(listBoxItem));
        }

        internal virtual void NotifyListItemLoaded(ListBoxItem listBoxItem)
        {
            if (ItemContainerGenerator.ItemFromContainer(listBoxItem) == SelectedItem)
            {
                listBoxItem.IsSelected = true;
                listBoxItem.Focus();
            }
        }

        internal virtual void NotifyListItemGotFocus(ListBoxItem listBoxItemNewFocus)
        {
        }

        internal virtual void NotifyListItemLostFocus(ListBoxItem listBoxItemOldFocus)
        {
        }

        private static void OnIsSynchronizedWithCurrentItemChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((Selector)o).IsSynchronizedWithCurrentItemChanged(o, e);
        }

        private static void OnSelectedIndexChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((Selector)o).SelectedIndexChanged(o, e);
        }

        private static void OnSelectedValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((Selector)o).SelectedValueChanged(o, e);
        }

        void ISupportInitialize.BeginInit()
        {
            Initializing = true;

            InitState = new State
            {
                Index = SelectedIndex,
                Item = SelectedItem,
                Value = SelectedValue,
                ValuePath = SelectedValuePath,
            };
        }

        void ISupportInitialize.EndInit()
        {
            Initializing = false;

            if (SelectedValue != InitState.Value)
            {
                SelectItemFromValue(SelectedValueWalker == null ? SelectedValue : SelectedValueWalker.Value, false);
            }
            else if (SelectedIndex != InitState.Index)
            {
                Selection.Select(SelectedIndex < Items.Count ? Items[SelectedIndex] : null);
            }
            else if (SelectedItem != InitState.Item)
            {
                Selection.Select(SelectedItem);
            }
            else if (SelectedValuePath != InitState.ValuePath)
            {
                SelectItemFromValue(SelectedValueWalker == null ? SelectedValue : SelectedValueWalker.Value, false);
            }

            InitState = null;
        }

        private static void OnSelectedItemChanged_cb(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((Selector)o).SelectedItemChanged(o, e);
        }

        private static void OnSelectedValuePathChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var selector = (Selector)o;

            var value = (string)e.NewValue;
            
            selector.SelectedValueWalker = string.IsNullOrEmpty(value) ? null : new PropertyPathWalker(value);

            if (selector.Initializing)
            {
                return;
            }

            selector.SelectItemFromValue(selector.SelectedValue, true);
        }

        private class State
        {
            public int Index { get; set; }

            public object Item { get; set; }

            public object Value { get; set; }

            public string ValuePath { get; set; }
        }
    }
}
