﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using UIElement = System.Windows.UIElement;

namespace FileDissector.Views
{
    public class ScrollValues
    {
        public int Rows { get; }
        public int FirstIndex { get; }

        public ScrollValues(int rows, int firstIndex)
        {
            Rows = rows;
            FirstIndex = firstIndex;
        }
    }

    public interface IScrollReceiver
    {
        void RequestChange(ScrollValues values);
    }

    public class VirtualDataPanel : VirtualizingPanel, IScrollInfo
    {
        private const double ScrollLineAmount = 16.0;
        private Size _extentSize;
        private ExtentInfo _extentInfo = new ExtentInfo();
        private Size _viewPortSize;
        private Point _offset;
        private ItemsControl _itemsControl;
        private readonly Dictionary<UIElement, Rect> _childLayouts = new Dictionary<UIElement, Rect>();
        private IRecyclingItemContainerGenerator _itemsGenerator;
        private bool _isInMeasure;
        private int _size;
        private int _firstIndex;

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(double), typeof(VirtualDataPanel), new PropertyMetadata(1.0, OnRequireMeasure));

        public static readonly DependencyProperty VirtualItemIndexProperty = 
            DependencyProperty.RegisterAttached("VirtualItemIndex", typeof(int), typeof(VirtualDataPanel), new PropertyMetadata(-1));
        
        public static readonly DependencyProperty TotalItemsProperty =
            DependencyProperty.Register("TotalItems", typeof(int), typeof(VirtualDataPanel), new PropertyMetadata(default(int), OnRequireMeasure));

        public static readonly DependencyProperty StartIndexProperty =
            DependencyProperty.Register("StartIndex", typeof(int), typeof(VirtualDataPanel), new PropertyMetadata(default(int), OnStartIndexChanged));

        public static readonly DependencyProperty ScrollReceiverProperty =
            DependencyProperty.Register("ScrollReceiver", typeof(IScrollReceiver), typeof(VirtualDataPanel), new PropertyMetadata(default(IScrollReceiver)));


        public static readonly DependencyProperty ChangeStartIndexCommandProperty =
            DependencyProperty.Register("ChangeStartIndexCommand", typeof(ICommand), typeof(VirtualDataPanel), new PropertyMetadata(default(ICommand)));

        public static readonly DependencyProperty ChangeSizeCommandProperty =
            DependencyProperty.Register("ChangeSizeCommand", typeof(ICommand), typeof(VirtualDataPanel), new PropertyMetadata(default(ICommand)));

        private static void OnStartIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as VirtualDataPanel;
            panel?.InvalidateMeasure();
            panel?.InvalidateScrollInfo();
        }

        private static void OnRequireMeasure(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as VirtualDataPanel;
            panel?.InvalidateMeasure();
            panel?.InvalidateScrollInfo();
        }

        public IScrollReceiver ScrollReceiver
        {
            get => (IScrollReceiver)GetValue(ScrollReceiverProperty);
            set => SetValue(ScrollReceiverProperty, value);
        }

        public ICommand ChangeSizeCommand
        {
            get => (ICommand)GetValue(ChangeSizeCommandProperty);
            set => SetValue(ChangeSizeCommandProperty, value);
        }

        public ICommand ChangeStartIndexCommand     
        {
            get => (ICommand)GetValue(ChangeStartIndexCommandProperty);
            set => SetValue(ChangeStartIndexCommandProperty, value);
        }

        public int StartIndex
        {
            get => (int)GetValue(StartIndexProperty);
            set => SetValue(StartIndexProperty, value);
        }


        public int TotalItems
        {
            get => (int)GetValue(TotalItemsProperty);
            set => SetValue(TotalItemsProperty, value);
        }

        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        private static int GetVirtualItemIndex(DependencyObject obj)
        {
            return (int) obj.GetValue(VirtualItemIndexProperty);
        }

        private static void SetVirtualItemIndex(DependencyObject obj, int value)
        {
            obj.SetValue(VirtualItemIndexProperty, value);
        }

        public double ItemWidth => _extentSize.Width;

        public VirtualDataPanel()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Dispatcher?.BeginInvoke(new Action(Initialize));
            }
        }

        private void Initialize()
        {
            _itemsControl = ItemsControl.GetItemsOwner(this);
            _itemsGenerator = (IRecyclingItemContainerGenerator) ItemContainerGenerator;

            InvalidateMeasure();
        }

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            base.OnItemsChanged(sender, args);

            InvalidateMeasure();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (!sizeInfo.HeightChanged) return;

            var items = (int) (sizeInfo.NewSize.Height / ItemHeight);
            InvokeSizeCommand(items);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_itemsControl == null) return availableSize;

            _isInMeasure = true;
            _childLayouts.Clear();

            var extentInfo = GetVerticalExtentInfo(availableSize);
            _extentInfo = extentInfo;

            EnsureScrollOffsetIsWithinConstrains(extentInfo);

            var layoutInfo = GetLayoutInfo(availableSize, ItemHeight, extentInfo);

            RecycleItems(layoutInfo);

            // determine where the first items is in relation to previously realized items
            var generatorStartPosition = _itemsGenerator.GeneratorPositionFromIndex(layoutInfo.FirstRealizedItemIndex);

            var visualIndex = 0;
            var actualWidth = 0.0;
            var currentX = layoutInfo.FirstRealizedItemLeft;
            var currentY = layoutInfo.FirstRealizedLineTop;

            using (_itemsGenerator.StartAt(generatorStartPosition, GeneratorDirection.Forward, true))
            {
                for (var itemIndex = layoutInfo.FirstRealizedItemIndex; itemIndex <= layoutInfo.LastRealizedItemIndex; itemIndex++,visualIndex++)
                {
                    var child = (UIElement) _itemsGenerator.GenerateNext(out var newlyRealized);

                    if (child == null) continue;

                    SetVirtualItemIndex(child, itemIndex);

                    if (newlyRealized)
                    {
                        InsertInternalChild(visualIndex, child);
                    }
                    else
                    {
                        // check if item needs to be moved into a new position in the children collection
                        if (visualIndex < Children.Count)
                        {
                            if (!Equals(Children[visualIndex], child))
                            {
                                var childCurrentIndex = Children.IndexOf(child);
                                if (childCurrentIndex >= 0)
                                {
                                    RemoveInternalChildRange(childCurrentIndex, 1);
                                }
                                InsertInternalChild(visualIndex, child);
                            }
                        }
                        else
                        {
                            // we know that the child can't already be in the children collection because we've been inserting children
                            // in the correct visual index order, and this child has a visual index greater than the Childrent.Count
                            AddInternalChild(child);
                        }
                    }

                    // only prepare the item once it has been added to the visaul tree
                    _itemsGenerator.PrepareItemContainer(child);
                    child.Measure(new Size(double.PositiveInfinity, ItemHeight));
                    actualWidth = _viewPortSize.Width;

                    _childLayouts.Add(child, new Rect(currentX, currentY, actualWidth, ItemHeight));
                    currentY += ItemHeight;
                }
            }

            RemoveRedundantChildren();
            UpdateScrollInfo(availableSize, extentInfo, actualWidth);

            var desiredSize = new Size(
                double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);

            _isInMeasure = false;

            return desiredSize;
        }

        private void EnsureScrollOffsetIsWithinConstrains(ExtentInfo extentInfo)
        {
            _offset.Y = Clamp(_offset.Y, 0, extentInfo.MaxVerticalOffset);
        }

        private void RecycleItems(ItemLayoutInfo layoutInfo)
        {
            foreach (UIElement child in Children)
            {
                var virtualItemIndex = GetVirtualItemIndex(child);

                if (virtualItemIndex < layoutInfo.FirstRealizedItemIndex || virtualItemIndex > layoutInfo.LastRealizedItemIndex)
                {
                    var generatorPosition = _itemsGenerator.GeneratorPositionFromIndex(virtualItemIndex);

                    if (generatorPosition.Index >= 0)
                    {
                        _itemsGenerator.Recycle(generatorPosition, 1);
                    }
                }

                SetVirtualItemIndex(child, -1);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in Children)
            {
                child.Arrange(_childLayouts[child]);
            }

            return finalSize;
        }

        private void UpdateScrollInfo(Size availableSize, ExtentInfo extentInfo, double actualWidth)
        {
            _viewPortSize = availableSize;
            _extentSize = new Size(actualWidth, extentInfo.Height);
        }

        private void InvalidateScrollInfo()
        {
            ScrollOwner?.InvalidateScrollInfo();
        }

        private void RemoveRedundantChildren()
        {
            // iterate backwards through the child collection because we are going to be remove items from it
            for (int i = Children.Count -1; i >= 0; i--)
            {
                var child = Children[i];

                // if the virtual item index is -1, this indicates that it is a recyled item that hasn't been used this time around
                if (GetVirtualItemIndex(child) == -1)
                {
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        private ItemLayoutInfo GetLayoutInfo(Size availableSize, double itemHeight, ExtentInfo extentInfo)
        {
            if (_itemsControl == null) return new ItemLayoutInfo();

            // we need to ensure that there is one realized item prior to the first visible item, and after the last visible ite, so that 
            // keyword navigation works properly. For example, when focus is on the first visible item, and the user navigates up, the ListBox selects the 
            // previous item, and scrolls that into view - and this triggers the loading of the rest of the items in that row

            var firstVisibleLine = (int) Math.Floor(_offset.Y / itemHeight);

            var firstRealizedIndex = Math.Max(firstVisibleLine - 1, 0);
            var firstRealizedItemLeft = firstRealizedIndex * ItemWidth - HorizontalOffset;
            var firstRealizedItemTop = firstRealizedIndex * itemHeight - _offset.Y;

            var firstCompleteLineTop =
                firstRealizedIndex == 0 ? firstRealizedItemTop : firstRealizedItemTop + ItemHeight;
            var completeRealizedLine = (int) Math.Ceiling((availableSize.Height - firstCompleteLineTop) / itemHeight);

            var lastRealizedIndex =
                Math.Min(firstRealizedIndex + completeRealizedLine + 2, _itemsControl.Items.Count - 1);

            return new ItemLayoutInfo()
            {
                FirstRealizedItemIndex =  firstRealizedIndex,
                FirstRealizedItemLeft =  firstRealizedItemLeft,
                FirstRealizedLineTop = firstRealizedItemTop,
                LastRealizedItemIndex = lastRealizedIndex
            };
        }

        private ExtentInfo GetVerticalExtentInfo(Size viewPortSize)
        {
            if (_itemsControl == null) return new ExtentInfo();

            var extentHeight = Math.Max(TotalItems * ItemHeight, viewPortSize.Height);
            var maxVerticalOffset = extentHeight - viewPortSize.Height;
            var verticalOffset = (StartIndex / (double) TotalItems) * maxVerticalOffset;
                
            var info = new ExtentInfo()
            {
                VirtualCount = _itemsControl.Items.Count,
                VerticalOffset = verticalOffset,
                TotalCount = TotalItems,
                Height = extentHeight,
                MaxVerticalOffset = maxVerticalOffset
            };

            return info;
        }

        

        public void SetHorizontalOffset(double offset)
        {
            offset = Clamp(offset, 0, ExtentWidth - ViewportWidth);

            if (offset < 0)
            {
                _offset.X = 0;
            }
            else
            {
                _offset = new Point(offset, _offset.Y);
            }

            InvalidateScrollInfo();
            InvalidateMeasure();
        }

        public void SetVerticalOffset(double offset)
        {
            if (double.IsInfinity(offset)) return;

            var diff = (int) ((offset - _extentInfo.VerticalOffset) / ItemHeight);
            InvokeStartIndexCommand(diff);
        }

        private double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        private void InvokeStartIndexCommand(int lines)
        {
            if (_isInMeasure) return;

            var firstIndex = StartIndex + lines;
            if (firstIndex < 0)
            {
                firstIndex = 0;
            }
            else if (firstIndex + _extentInfo.VirtualCount > _extentInfo.TotalCount)
            {
                firstIndex = _extentInfo.TotalCount - _extentInfo.VirtualCount;
            }

            if (firstIndex == _firstIndex) return;

            _firstIndex = firstIndex;
            ScrollReceiver?.RequestChange(new ScrollValues(_size, _firstIndex + 1));
        }

        private void InvokeSizeCommand(int size)
        {
            _size = size;
            ScrollReceiver?.RequestChange(new ScrollValues(size, _firstIndex + 1));
        }

        public double ExtentWidth => _extentSize.Width;
        public double ExtentHeight => _extentSize.Height;
        public double ViewportWidth => _viewPortSize.Width;
        public double ViewportHeight => _viewPortSize.Height;
        public bool CanVerticallyScroll { get; set; }
        public bool CanHorizontallyScroll { get; set; }
        public double HorizontalOffset => _offset.X;
        public double VerticalOffset => _offset.Y + _extentInfo.VerticalOffset;
        public ScrollViewer ScrollOwner { get; set; }

        public void LineUp()
        {
            //InvokeStartIndexCommand(-1);
        }

        public void LineDown()
        {
            //InvokeStartIndexCommand(1);
        }

        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset + ScrollLineAmount);
        }

        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset - ScrollLineAmount);
        }

        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - ViewportHeight);
        }

        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + ViewportHeight);
        }

        public void PageLeft()
        {
            SetHorizontalOffset(HorizontalOffset + ItemWidth);
        }

        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset - ItemWidth);
        }

        public void MouseWheelUp()
        {
            InvokeStartIndexCommand(-SystemParameters.WheelScrollLines);
        }

        public void MouseWheelDown()
        {
            InvokeStartIndexCommand(SystemParameters.WheelScrollLines);
        }

        public void MouseWheelLeft()
        {
            SetHorizontalOffset(HorizontalOffset - ScrollLineAmount * SystemParameters.WheelScrollLines);
        }

        public void MouseWheelRight()
        {
            SetHorizontalOffset(HorizontalOffset + ScrollLineAmount * SystemParameters.WheelScrollLines);
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return new Rect();
        }

        private class ExtentInfo
        {
            public int TotalCount;
            public int VirtualCount;
            public double VerticalOffset;
            public double MaxVerticalOffset;
            public double Height;
        }

        private class ItemLayoutInfo
        {
            public int FirstRealizedItemIndex;
            public double FirstRealizedLineTop;
            public double FirstRealizedItemLeft;
            public int LastRealizedItemIndex;
        }
    }
}
