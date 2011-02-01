using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CNNWB.Model;

namespace CNNWB
{
    public class ConfusionMatrix : ObservableCollection<ConfusionItems>
    {
        private string _name;
        private DataProvider _dataProvider;

        public ConfusionMatrix(string name, DataProvider dataProvider)
            : base()
        {
            DataProvider = dataProvider;
            Name = name;
            

            for (int i=0; i < 10; i++)
                base.Add (new ConfusionItems (i,DataProvider));
        }

        public String Name
        {
            get { return _name; }
            set
            {
                if (value == _name)
                    return;

                _name = value;
                base.OnPropertyChanged(new PropertyChangedEventArgs("Name"));
            }
        }

        public DataProvider DataProvider
        {
            get {return _dataProvider;}
            set
            {
                if (value == _dataProvider)
                    return;

                _dataProvider = value;
                base.OnPropertyChanged (new PropertyChangedEventArgs ("DataProvider"));
            }
        }
   
    }

    public class ConfusionItems : TabItem, INotifyPropertyChanged 
    {
        private ObservableCollection<ConfusedItem> _items;
        private int _correctValue;
        private DataProvider _dataProvider;
        private WrapPanel panel;
        private ScrollViewer scr;
        
        public int CorrectValue 
        { 
            get
            {
                return _correctValue;
            } 

            private set
            {
                if (value == _correctValue)
                    return;
 
                _correctValue = value;

                this.OnPropertyChanged ("CorrectValue");
            }
        }

        public DataProvider DataProvider
        {
            get
            {
                return _dataProvider;
            }

            private set
            {
                if (value == _dataProvider)
                    return;

                _dataProvider = value;
                this.OnPropertyChanged("DataProvider");
            }
        }

        public ObservableCollection<ConfusedItem> Items
        {
            get
            {
                return (_items);
            }

            private set
            {
                if (value == _items)
                    return;

                _items = value;

                this.OnPropertyChanged("Items");
            }
        }

        public ConfusionItems(int correctValue, DataProvider dataProvider)
            : base()
        {
            DataProvider = dataProvider;
            CorrectValue = correctValue;
            Items = new ObservableCollection<ConfusedItem>();
            panel = new WrapPanel();
            scr = new ScrollViewer();
            GetConfusedItems();
            Items.CollectionChanged += new NotifyCollectionChangedEventHandler(Items_CollectionChanged);
        }

        ~ConfusionItems()
        {
            Items.CollectionChanged -= new NotifyCollectionChangedEventHandler(Items_CollectionChanged);
            Items = null;
        }

        public void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            GetConfusedItems();
        }

        public void GetConfusedItems()
        {
            Content = null;
            panel.SnapsToDevicePixels = true;
            //panel.Background = System.Windows.Media.Brushes.White;
            panel.Orientation = Orientation.Horizontal;
            panel.HorizontalAlignment = HorizontalAlignment.Stretch;
            panel.VerticalAlignment = VerticalAlignment.Stretch;
            panel.BeginInit();
            foreach (ConfusedItem wrongImage in Items)
            {
                if ((wrongImage != null) && !panel.Children.Contains(wrongImage))
                {
                    panel.Children.Add(wrongImage);
                }
            }
            panel.EndInit();

            scr.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            scr.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scr.HorizontalContentAlignment = HorizontalAlignment.Left;
            scr.VerticalContentAlignment = VerticalAlignment.Top;
            scr.VerticalAlignment = VerticalAlignment.Stretch;
            scr.HorizontalAlignment = HorizontalAlignment.Stretch;
            scr.Content = panel;
            scr.Height = double.NaN;
            scr.Width = double.NaN;

            Header = CorrectValue.ToString() + " (" + Items.Count.ToString () + ")";
            Content = scr;
            
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }

    public class ConfusedItem: Grid, INotifyPropertyChanged 
    {
        private int _patternIndex;
        private int _wrongValue;
        private DataProvider _dataProvider;

        public int PatternIndex 
        { 
            get 
            {
                return _patternIndex;
            }
            private set
            {
                if (value == _patternIndex)
                    return;

                _patternIndex = value;
                this.OnPropertyChanged("PatternIndex");
            }
        }
        
        public int WrongValue 
        {
            get
            {
                return _wrongValue;
            }
            private set
            {
                if (value == _wrongValue)
                    return;

                _wrongValue = value;
                this.OnPropertyChanged("WrongValue");
            }
        }
        
        public DataProvider DataProvider 
        {
            get
            {
                return _dataProvider;
            }
            private set
            {
                if (value == _dataProvider)
                    return;

                _dataProvider = value;
                this.OnPropertyChanged("DataProvider");
            }
        }
        
        public ConfusedItem(int patternIndex, int wrongValue, bool useTestingSet, DataProvider dataProvider)
        {
            DataProvider = dataProvider;
            PatternIndex = patternIndex;
            WrongValue = wrongValue;


            Background = System.Windows.Media.Brushes.White;
            SnapsToDevicePixels = true;
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            Width = 78;
            Height = 78;
            Margin = new Thickness(2);

            ColumnDefinitions.Add(new ColumnDefinition());
            RowDefinition row0 = new RowDefinition();
            row0.Height = new GridLength(34);
            RowDefinitions.Add(row0);
            RowDefinition row1 = new RowDefinition();
            row1.Height = new GridLength(0, GridUnitType.Auto);
            RowDefinitions.Add(row1);
            RowDefinition row2 = new RowDefinition();
            row2.Height = new GridLength(0, GridUnitType.Auto);
            RowDefinitions.Add(row2);

            System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
            rectangle.HorizontalAlignment = HorizontalAlignment.Stretch;
            rectangle.VerticalAlignment = VerticalAlignment.Stretch;
            rectangle.Margin = new Thickness(1);
            rectangle.Height = double.NaN;
            rectangle.Width = double.NaN;
            rectangle.SnapsToDevicePixels = true;
            rectangle.Stretch = Stretch.UniformToFill;
            rectangle.StrokeThickness = 1;
            rectangle.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
            rectangle.Fill = System.Windows.Media.Brushes.Transparent;

            Grid.SetColumn(rectangle, 0);
            Grid.SetRow(rectangle, 0);
            Grid.SetRowSpan(rectangle, 3);

            System.Windows.Controls.Image wImage = new System.Windows.Controls.Image();
            wImage.BeginInit();
            wImage.Source = ShowInputPattern(PatternIndex, useTestingSet);
            wImage.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            wImage.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            wImage.Stretch = System.Windows.Media.Stretch.None;
            wImage.SnapsToDevicePixels = true;
            wImage.MinHeight = 32;
            wImage.MaxHeight = 32;
            wImage.MinWidth = 32;
            wImage.MaxWidth = 32;
            wImage.Margin = new Thickness(1);
            wImage.EndInit();
            Grid.SetColumn(wImage, 0);
            Grid.SetRow(wImage, 0);

            TextBlock patternIndexTextBlock = new TextBlock();
            patternIndexTextBlock.FontSize = 11;
            patternIndexTextBlock.TextAlignment = System.Windows.TextAlignment.Center;
            patternIndexTextBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            patternIndexTextBlock.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            patternIndexTextBlock.Margin = new Thickness(1);
            patternIndexTextBlock.Text = PatternIndex.ToString(CultureInfo.CurrentUICulture);
            patternIndexTextBlock.Height = double.NaN;
            patternIndexTextBlock.Width = double.NaN;
            Grid.SetColumn(patternIndexTextBlock, 0);
            Grid.SetRow(patternIndexTextBlock, 1);

            TextBlock wrongValueTextBlock = new TextBlock();
            wrongValueTextBlock.FontSize = 11;
            wrongValueTextBlock.TextAlignment = System.Windows.TextAlignment.Center;
            wrongValueTextBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            wrongValueTextBlock.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            wrongValueTextBlock.Margin = new Thickness(1);
            wrongValueTextBlock.Text = WrongValue.ToString(CultureInfo.CurrentUICulture);
            wrongValueTextBlock.Height = double.NaN;
            wrongValueTextBlock.Width = double.NaN;
            Grid.SetColumn(wrongValueTextBlock, 0);
            Grid.SetRow(wrongValueTextBlock, 2);

            //BeginInit();
            Children.Add(rectangle);
            Children.Add(wImage);
            Children.Add(patternIndexTextBlock);
            Children.Add(wrongValueTextBlock);
            //EndInit();
        }

        private BitmapSource ShowInputPattern(int patternIndex, bool useTestingSet)
        {
            // Define parameters used to create the BitmapSource.
            PixelFormat pf = PixelFormats.Gray8;
            int rawStride = (DataProvider.PatternWidth * pf.BitsPerPixel + 7) / 8;
            byte[] rawImage = new byte[rawStride * DataProvider.PatternHeight];
            DoubleImageData pattern;

            if (useTestingSet)
                pattern = DataProvider.TestingPatterns[patternIndex];
            else
                pattern = DataProvider.TrainingPatterns[patternIndex];
            
            if (DataProvider.FlipGrayscale)
            {
                for (int i = 0; i < DataProvider.PatternSize; ++i)
                {
                    rawImage[i] = (byte)((pattern.Image[i] + 1) * DataProvider.Divisor);
                }
            }
            else
            {
                for (int i = 0; i < DataProvider.PatternSize; ++i)
                {
                    rawImage[i] = (byte)(255 - ((pattern.Image[i] + 1) * DataProvider.Divisor));
                }
            }

            return (BitmapSource.Create(DataProvider.PatternWidth, DataProvider.PatternHeight, 96, 96, pf, null, rawImage, rawStride));
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }
}
