﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace ZhangPeng.Controls
{
    public sealed class CarouselView : Control
    {

        #region Properties

        #region SelectedIndex


        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(CarouselView), new PropertyMetadata(0));


        #endregion

        #region ItemWidth

        /// <summary>
        /// Get or Set the ItemWidth in CarouselView and it's suggested to to larger than 300
        /// </summary>
        public double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register("ItemWidth", typeof(double), typeof(CarouselView), new PropertyMetadata(300.00d));


        #endregion

        #region ItemImageSource


        public List<string> ItemImageSource
        {
            get { return (List<string>)GetValue(ItemImageSourceProperty); }
            set { SetValue(ItemImageSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemImageSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemImageSourceProperty =
            DependencyProperty.Register("ItemImageSource", typeof(List<string>), typeof(CarouselView), new PropertyMetadata(null,(s,e)=> 
            {
                if (e.NewValue!=e.OldValue)
                {
                    (s as CarouselView).SetItemsImageSource(true);
                }
            }));


        #endregion

        #endregion

        Compositor _compositor;
        //ContainerVisual _touchAreaVisual;
        Visual _touchAreaVisual, _indicatorVisual;
        List<Visual> _itemVisualList;
        List<CarouselViewItem> _itemUIElementList;
        ExpressionAnimation _animation, _animation_0, _animation_1, _animation_2, _animation_3, _animation_4;
        ScalarKeyFrameAnimation _indicatorAnimation;
        float _x;
        int _selectedIndex;
        Panel _canvas, _rootGrid;

        public CarouselView()
        {
            this.DefaultStyleKey = typeof(CarouselView);
            _selectedIndex = 2;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Get the original elements from the template
            _rootGrid = this.GetTemplateChild("RootGrid") as Panel;
            _canvas = this.GetTemplateChild("canvas") as Panel;
            var indiRect = this.GetTemplateChild("indicatorRect") as Rectangle;
            _indicatorVisual = ElementCompositionPreview.GetElementVisual(indiRect);
            _touchAreaVisual = ElementCompositionPreview.GetElementVisual(_canvas);
            _compositor = _touchAreaVisual.Compositor;
            _itemVisualList = new List<Visual>();
            _itemUIElementList = _canvas.GetDescendantsOfType<CarouselViewItem>().ToList();
            foreach (var item in _itemUIElementList)
            {
                _itemVisualList.Add(ElementCompositionPreview.GetElementVisual(item));
            }
            
            // Event handlers
            this._canvas.ManipulationMode = ManipulationModes.TranslateX;

            _canvas.ManipulationStarted += Canvas_ManipulationStarted;
            _canvas.ManipulationDelta += Canvas_ManipulationDelta;
            _canvas.ManipulationCompleted += Canvas_ManipulationCompleted;
            //_canvas.ManipulationInertiaStarting += Canvas_ManipulationInertiaStarting;

            _canvas.PointerWheelChanged += Canvas_PointerWheelChanged;
            // Response to the SizeChanged
            _rootGrid.SizeChanged += (ss, ee) => 
            {
                MeasureItemsPosition(_selectedIndex);
                // Change the Clip to fit new size
                _canvas.Clip = new RectangleGeometry() { Rect = RectHelper.FromCoordinatesAndDimensions(0, 0, (float)ee.NewSize.Width, (float)ee.NewSize.Height) };
            };
            //_canvas.SizeChanged += (ss, ee) => { MeasureItemsPosition(_selectedIndex, _selectedIndex); };
            this.Loaded += (ss,ee) => 
            {
                // Initial items' image data
                SetItemsImageSource(true);
                SetSelectedAppearance();
                //MeasureItemsPosition(_selectedIndex);
            };
        }
        private void PrepareAnimations()
        {
            _animation = _compositor.CreateExpressionAnimation("touch.Offset.X");
            _animation.SetReferenceParameter("touch", _indicatorVisual);

            _animation_0 = _compositor.CreateExpressionAnimation("touch.Offset.X+self");
            float offsestX_0 = _itemVisualList[0].Offset.X;
            _animation_0.SetScalarParameter("self", offsestX_0);
            _animation_0.SetReferenceParameter("touch", _indicatorVisual);

            _animation_1 = _compositor.CreateExpressionAnimation("touch.Offset.X+self");
            float offsestX_1 = _itemVisualList[1].Offset.X;
            _animation_1.SetScalarParameter("self", offsestX_1);
            _animation_1.SetReferenceParameter("touch", _indicatorVisual);

            _animation_2 = _compositor.CreateExpressionAnimation("touch.Offset.X+self");
            float offsestX_2 = _itemVisualList[2].Offset.X;
            _animation_2.SetScalarParameter("self", offsestX_2);
            _animation_2.SetReferenceParameter("touch", _indicatorVisual);

            _animation_3 = _compositor.CreateExpressionAnimation("touch.Offset.X+self");
            float offsestX_3 = _itemVisualList[3].Offset.X;
            _animation_3.SetScalarParameter("self", offsestX_3);
            _animation_3.SetReferenceParameter("touch", _indicatorVisual);

            _animation_4 = _compositor.CreateExpressionAnimation("touch.Offset.X+self");
            float offsestX_4 = _itemVisualList[4].Offset.X;
            _animation_4.SetScalarParameter("self", offsestX_4);
            _animation_4.SetReferenceParameter("touch", _indicatorVisual);
        }

        private void SetItemsImageSource(bool isinitial=false)
        {
            System.Diagnostics.Debug.WriteLine("SetItemsImageSource");
            // Set the imagesource of each item. 
            if (ItemImageSource==null)
            {
                return;
            }
            if (ItemImageSource.Count == 0) return;

            // get the source indexes
            int count = ItemImageSource.Count;
            int sindex = SelectedIndex;
            int sindex_0, sindex_1, sindex_2, sindex_3, sindex_4;
            sindex_0 = sindex - 2 < 0 ? (sindex - 2) + count : sindex - 2;
            sindex_1 = (sindex - 1) < 0 ? (sindex - 1) + count : sindex - 1;
            sindex_2 = sindex;
            sindex_3 = (sindex + 1) > count - 1 ? (sindex + 1) - count : sindex + 1;
            sindex_4 = (sindex + 2) > count - 1 ? (sindex + 2) - count : sindex + 2;

            // if count=1 all the sindexes = 0
            if (count==1)
            {
                sindex_0 = sindex_1 = sindex_2 = sindex_3 = sindex_4 = 0;
            }
            // get the UIelement indexes
            int index_0, index_1, index_2, index_3, index_4;
            index_0 = _selectedIndex - 2;
            if (index_0 < 0) index_0 = index_0 + 5;
            index_1 = _selectedIndex - 1;
            if (index_1 < 0) index_1 = index_1 + 5;
            index_2 = _selectedIndex;
            index_3 = _selectedIndex + 1;
            if (index_3 > 4) index_3 = index_3 - 5;
            index_4 = _selectedIndex + 2;
            if (index_4 > 4) index_4 = index_4 - 5;

            _itemUIElementList[index_0].ImageSource = new BitmapImage(new Uri(ItemImageSource[sindex_0]));
            // To avoid to flash (reason is unclear).
            if (isinitial)
            {
                _itemUIElementList[index_1].ImageSource = new BitmapImage(new Uri(ItemImageSource[sindex_1]));
                _itemUIElementList[index_2].ImageSource = new BitmapImage(new Uri(ItemImageSource[sindex_2]));
                _itemUIElementList[index_3].ImageSource = new BitmapImage(new Uri(ItemImageSource[sindex_3]));
            }
            _itemUIElementList[index_4].ImageSource = new BitmapImage(new Uri(ItemImageSource[sindex_4]));

        }

        private void SetSelectedAppearance()
        {
            // Set the Selected or UnSelected Items' stype
            // Like BlackMaskOpacity of CarouselViewItem
            for (int i = 0; i < 5; i++)
            {
                if (i==_selectedIndex)
                {
                    _itemUIElementList[i].BlackMaskOpacity = 0;
                }
                else
                {
                    _itemUIElementList[i].BlackMaskOpacity = 0.3;
                }
            }
        }

        private void Canvas_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Canvas_ManipulationInertiaStarted");
            // Stop the Animation and reset the postion of indicator
            _itemVisualList[0].StopAnimation("Offset.X");
            _itemVisualList[1].StopAnimation("Offset.X");
            _itemVisualList[2].StopAnimation("Offset.X");
            _itemVisualList[3].StopAnimation("Offset.X");
            _itemVisualList[4].StopAnimation("Offset.X");
            _x = 0.0f;
            _indicatorVisual.Offset = new Vector3(_x, 0.0f, 0.0f);

            // Prepare animiations
            PrepareAnimations();

            // Strat the Animations based on the indicator
            _itemVisualList[0].StartAnimation("Offset.X", _animation_0);
            _itemVisualList[1].StartAnimation("Offset.X", _animation_1);
            _itemVisualList[2].StartAnimation("Offset.X", _animation_2);
            _itemVisualList[3].StartAnimation("Offset.X", _animation_3);
            _itemVisualList[4].StartAnimation("Offset.X", _animation_4);
        }
        private void Canvas_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            _x += (float)e.Delta.Translation.X;

            // set the pan rectangle's visual's offset
            _indicatorVisual.Offset = new Vector3(_x, 0.0f, 0.0f);

        }

        //private void Canvas_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        //{
        //    System.Diagnostics.Debug.WriteLine("Canvas_ManipulationInertiaStarting");
        //}
        private void Canvas_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            double containerWidth = _canvas.ActualWidth;
            double itemWidth = _itemUIElementList[0].ActualWidth;
            double cleft = (containerWidth - itemWidth) / 2;
            // evaluate the new selectedIndex
            int oldSelectedIndex = _selectedIndex;
            var cha = _indicatorVisual.Offset.X - cleft;
            if (cha <= -cleft)
            {
                // new selcted item is the right item of current item
                _selectedIndex = _selectedIndex + 1;
                if (_selectedIndex > 4)
                {
                    _selectedIndex = _selectedIndex - 5;
                }
                // Change the SelectedIndex
                var k = SelectedIndex + 1;
                SelectedIndex = k > ItemImageSource.Count-1 ? k - ItemImageSource.Count : k;
            }
            if (cha >= cleft)
            {
                // new selcted item is the left item of current item
                _selectedIndex = _selectedIndex - 1;
                if (_selectedIndex < 0) _selectedIndex = _selectedIndex + 5;

                // Change the SelectedIndex
                var k = SelectedIndex - 1;
                SelectedIndex = k < 0 ? k + ItemImageSource.Count : k;
            }
            else
            {
                //_animation.InsertKeyFrame(1.0f, 0.0f);
            }

            MeasureItemsPosition(_selectedIndex, oldSelectedIndex);
            //foreach (var item in _itemVisualList)
            //{
            //    item.StartAnimation("Offset.X", _animation);
            //}
            //var backScopedBatch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            //_itemVisualList[4].StartAnimation("Offset.X", _animation);
            //backScopedBatch.End();
            //backScopedBatch.Completed += BackScopedBatch_Completed;
            //_visual.StartAnimation("Offset.X", _animation);
            //_touchAreaVisual.StartAnimation("Offset.X", _animation);
        }



        static ContainerVisual GetVisual(UIElement element)
        {
            var hostVisual = ElementCompositionPreview.GetElementVisual(element);
            ContainerVisual root = hostVisual.Compositor.CreateContainerVisual();
            ElementCompositionPreview.SetElementChildVisual(element, root);
            return root;
        }

        private void MeasureItemsPosition(int index, int oldindex = -1)
        {
            System.Diagnostics.Debug.WriteLine($"MeasureItemsPosition _selectedIndex is {index} _oldselected is {oldindex}");

            // Set the itemwidth
            // if the container width is larger than ItemWidtn, itemwidth = ItemWidth
            // else itemwidth = container
            double containerWidth = _canvas.ActualWidth;
            double itemWidth = _itemUIElementList[0].ActualWidth;
            if (containerWidth < this.ItemWidth)
            {
                foreach (var item in _itemUIElementList)
                {
                    item.Width = containerWidth;
                }
            }
            else if (itemWidth < ItemWidth)
            {
                foreach (var item in _itemUIElementList)
                {
                    item.Width = ItemWidth;
                }
            }
            itemWidth = _itemUIElementList[0].Width;
            double LLeft, Cleft, Rleft;
            Cleft = (containerWidth - itemWidth) / 2;
            LLeft = -(itemWidth - Cleft);
            Rleft = containerWidth - Cleft;

            int index_0, index_1, index_2, index_3, index_4;
            index_0 = index - 2;
            if (index_0 < 0) index_0 = index_0 + 5;
            index_1 = index - 1;
            if (index_1 < 0) index_1 = index_1 + 5;
            index_2 = index;
            index_3 = index + 1;
            if (index_3 > 4) index_3 = index_3 - 5;
            index_4 = index + 2;
            if (index_4 > 4) index_4 = index_4 - 5;

            // for initialing only, or no need to enable animiations
            if (oldindex == -1)
            {
                _itemVisualList[index_0].Offset = new Vector3((float)(LLeft - itemWidth), 0, 0);
                _itemVisualList[index_1].Offset = new Vector3((float)LLeft, 0, 0);
                _itemVisualList[index_2].Offset = new Vector3((float)Cleft, 0, 0);
                _itemVisualList[index_3].Offset = new Vector3((float)(Rleft), 0, 0);
                _itemVisualList[index_4].Offset = new Vector3((float)(Rleft + itemWidth), 0, 0);
                return;
            }

            int diff = index - oldindex;
            // new selected item equals to current item
            if (diff == 0)
            {
                _indicatorAnimation = _compositor.CreateScalarKeyFrameAnimation();
                _indicatorAnimation.InsertKeyFrame(1.0f, 0.0f);
                _indicatorAnimation.Duration = TimeSpan.FromMilliseconds(300);
            }
            // new selected item is the right item of current item
            if (diff == 1 || diff < -1)
            {
                _indicatorAnimation = _compositor.CreateScalarKeyFrameAnimation();
                _indicatorAnimation.InsertKeyFrame(1.0f, (float)-itemWidth);
                _indicatorAnimation.Duration = TimeSpan.FromMilliseconds(300);
            }
            // new selected item is the left one of current item
            if (diff == -1 || diff > 1)
            {
                _indicatorAnimation = _compositor.CreateScalarKeyFrameAnimation();
                _indicatorAnimation.InsertKeyFrame(1.0f, (float)itemWidth);
                _indicatorAnimation.Duration = TimeSpan.FromMilliseconds(300);
            }

            // Start the indicator animiation
            var backScopedBatch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            _indicatorVisual.StartAnimation("Offset.X", _indicatorAnimation);
            backScopedBatch.End();
            backScopedBatch.Completed += (ss, ee) =>
            {
                // reset the firt and last item's postion
                _itemVisualList[index_0].Offset = new Vector3((float)(LLeft - itemWidth), 0, 0);
                _itemVisualList[index_1].Offset = new Vector3((float)LLeft, 0, 0);
                _itemVisualList[index_2].Offset = new Vector3((float)Cleft, 0, 0);
                _itemVisualList[index_3].Offset = new Vector3((float)(Rleft), 0, 0);
                _itemVisualList[index_4].Offset = new Vector3((float)(Rleft + itemWidth), 0, 0);

                // Change item's imagesources
                SetItemsImageSource();
                // Set Selected Item's appearance
                SetSelectedAppearance();
                // Start wheel event capturing
                _canvas.PointerWheelChanged += Canvas_PointerWheelChanged;
            };

            // Set the ZIndex of the items
            //Canvas.SetZIndex(_itemUIElementList[index_0], 0);
            //Canvas.SetZIndex(_itemUIElementList[index_1], 1);
            //Canvas.SetZIndex(_itemUIElementList[index_2], 2);
            //Canvas.SetZIndex(_itemUIElementList[index_3], 1);
            //Canvas.SetZIndex(_itemUIElementList[index_4], 0);

           


        }

        private void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            // Cancel the wheel event capturing when starting processing
            e.Handled = true;
            _canvas.PointerWheelChanged -= Canvas_PointerWheelChanged;

            // Stop the Animation and reset the postion of indicator
            _itemVisualList[0].StopAnimation("Offset.X");
            _itemVisualList[1].StopAnimation("Offset.X");
            _itemVisualList[2].StopAnimation("Offset.X");
            _itemVisualList[3].StopAnimation("Offset.X");
            _itemVisualList[4].StopAnimation("Offset.X");
            _x = 0.0f;
            _indicatorVisual.Offset = new Vector3(_x, 0.0f, 0.0f);

            // Processing the PointerWheelChanged event by mouse
            var pointerpoint = e.GetCurrentPoint(_canvas);
            var mousewheeldelta = pointerpoint.Properties.MouseWheelDelta;

            int newindex = _selectedIndex;
            // MouseWheelDelta:
            // A positive value indicates that the wheel was rotated forward (away from the user) or tilted to the right;
            // A negative value indicates that the wheel was rotated backward (toward the user) or tilted to the left.
            if (mousewheeldelta > 0)
            {
                // get the index of last one
                newindex = _selectedIndex - 1;
                if (newindex < 0)
                {
                    newindex = newindex + 5;
                }
                // Change the SelectedIndex
                var k = SelectedIndex - 1;
                SelectedIndex = k <0  ? k + ItemImageSource.Count : k;
            }
            else if (mousewheeldelta < 0)
            {
                // get the index of next one
                newindex = _selectedIndex + 1;
                if (newindex > 4)
                {
                    newindex = newindex - 5;
                }
                // Change the SelectedIndex
                var k = SelectedIndex + 1;
                SelectedIndex = k > ItemImageSource.Count-1 ? k - ItemImageSource.Count : k;
            }
            PrepareAnimations();
            _itemVisualList[0].StartAnimation("Offset.X", _animation_0);
            _itemVisualList[1].StartAnimation("Offset.X", _animation_1);
            _itemVisualList[2].StartAnimation("Offset.X", _animation_2);
            _itemVisualList[3].StartAnimation("Offset.X", _animation_3);
            _itemVisualList[4].StartAnimation("Offset.X", _animation_4);
            // Changed the _selectedIndex
            int oldindex = _selectedIndex;
            _selectedIndex = newindex;
            MeasureItemsPosition(newindex, oldindex);

        }
    }
}
