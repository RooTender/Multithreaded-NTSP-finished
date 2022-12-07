using GUI.ViewModels;
using System.Runtime.InteropServices;
using System;
using System.Windows;
using System.Windows.Interop;

namespace GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    //[DllImport("user32.dll")]
    //static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
    //[DllImport("user32.dll")]
    //static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

    private const int GWL_STYLE = -16;
    private const int WS_SYSMENU = 0x80000;
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    const uint MF_GRAYED = 0x00000001;
    const uint MF_ENABLED = 0x00000000;
    const uint SC_CLOSE = 0xF060;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(this);
        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
    }
}
