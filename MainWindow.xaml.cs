using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BoringForm
{
    public static class Mouse
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        public static void SetCursorPos(double x, double y)
        {
            SetCursorPos((int)x, (int)y);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        public static System.Drawing.Point GetCursorPos()
        {
            GetCursorPos(out POINT pos);
            return new System.Drawing.Point(pos.X, pos.Y);
        }


        public static Cursor OverrideCursor
        {
            set { Mouse.OverrideCursor = value; }
        }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread? thread;
        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(WndProc);
        }

        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MOVE = 0xF010;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            switch (msg)
            {
                case WM_SYSCOMMAND:
                    int command = wParam.ToInt32() & 0xfff0;
                    if (command == SC_MOVE&&(this.BoringCheckBox.IsChecked??false))
                    {
                        handled = true;
                    }
                    break;
                default:
                    break;
            }
            return IntPtr.Zero;
        }

        private void MoveAndUncheck()
        {
            var targetRelPos = this.Dispatcher.Invoke(()=>new Point(this.BoringCheckBox.Margin.Left + 10,
                this.BoringCheckBox.Margin.Top + 10));
            Thread.Sleep(1000);
            while (this.Dispatcher.Invoke(()=>this.BoringCheckBox.IsChecked??false))
            {
                
                var target = this.Dispatcher.Invoke(() =>
                    PointToScreen(targetRelPos));
                var current = Mouse.GetCursorPos();
                var deltaX = target.X - current.X;
                var deltaY = target.Y - current.Y; 
                var distance = Math.Sqrt(deltaX*deltaX + deltaY*deltaY);
                double offset = 10+distance*0.05;
                if (distance < offset)
                {
                    Mouse.SetCursorPos(target.X,target.Y);
                    this.Dispatcher.Invoke(() => this.BoringCheckBox.IsChecked = false);
                    break;
                }

                var newX = (offset / distance) * deltaX + current.X;
                var newY = (offset / distance) * deltaY + current.Y;
                Mouse.SetCursorPos(newX,newY);
                Thread.Sleep(1);
            }

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
            this.Activate();
            if (thread is null)
            {
                thread = new Thread(MoveAndUncheck);
                thread.Start();
            }
        }

        private void MainWindow_OnStateChanged(object? sender, EventArgs e)
        {
            if (this.BoringCheckBox.IsChecked ?? false)
            {
                this.WindowState = WindowState.Normal;
            }
            
        }

        private void CheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost= false;
            thread = null;
        }

    }
}