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
    private readonly IntPtr _systemMenu;
    private const uint CloseMenuButton = 0xF060;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(this);

        var windowHandle = new IntPtr();
        Dispatcher.Invoke(() => { windowHandle = new WindowInteropHelper(this).Handle; });

        _systemMenu = GetSystemMenu(windowHandle, false);
    }

    public void DisableWindowClosing()
    {
        EnableMenuItem(_systemMenu, CloseMenuButton, 0x00000001);
    }

	public void EnableWindowClosing()
	{
        EnableMenuItem(_systemMenu, CloseMenuButton, 0x0);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
}
