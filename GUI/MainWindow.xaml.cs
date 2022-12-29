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

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(this);
    }

	[DllImport("user32.dll")]
	static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
	[DllImport("user32.dll")]
	static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

	const uint MF_GRAYED = 0x00000001;
	const uint MF_ENABLED = 0x00000000;
	const uint SC_CLOSE = 0xF060;

	public void tb_Click(bool disable)
	{
		IntPtr hwnd = new IntPtr();
		if (disable)
		{
			this.Dispatcher.Invoke((Action)(() => { hwnd = new WindowInteropHelper(this).Handle; }));
			IntPtr hMenu = GetSystemMenu(hwnd, false);
			EnableMenuItem(hMenu, SC_CLOSE, MF_GRAYED);
		}
		else
		{
			
			this.Dispatcher.Invoke((Action)(() => { hwnd = new WindowInteropHelper(this).Handle; }));
			
			IntPtr hMenu = GetSystemMenu(hwnd, false);
			EnableMenuItem(hMenu, SC_CLOSE, MF_ENABLED);
		}
	}
}
