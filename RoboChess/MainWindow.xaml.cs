using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RoboChess
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isBoardFlipped;
        private RotateTransform boardRotateTx;
        private DataTemplate boardTemplate;
        private bool isLoaded;

        public MainWindow()
        {
            InitializeComponent();
            boardRotateTx = new RotateTransform();    
            isLoaded = true;
        }

        private void FlipBoard(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            if (!isBoardFlipped)
            {
                boardRotateTx.Angle = 180;
                boardTemplate = (DataTemplate)FindResource("FlippedBoardTemplate");
                isBoardFlipped = true;
            }
            else
            {
                boardRotateTx.Angle = 0;
                boardTemplate = (DataTemplate)FindResource("BoardTemplate");
                isBoardFlipped = false;
            }
            BoardListBox.RenderTransform = boardRotateTx;
            BoardListBox.ItemTemplate = boardTemplate;
        }
    }
}
