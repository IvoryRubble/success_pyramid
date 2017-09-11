using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
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

namespace success_pyramid
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		string ImageFolderPath;
		FileInfo[] Images;
		int CurrentImageIndex = 0;
		SerialPort Port;
		bool Fullscreen = false;

		public MainWindow()
		{
			InitializeComponent();

			string[] ports = SerialPort.GetPortNames();
			PortComboBox.ItemsSource = SerialPort.GetPortNames();
			if (ports.Length > 0)
			{
				PortComboBox.SelectedItem = ports[0];
			}

			int[] baudRates = new int[] { 9600, 19200, 38400, 57600, 115200 };
			BaudRateComboBox.ItemsSource = baudRates;
			BaudRateComboBox.SelectedItem = baudRates[0];
		}

		private void OnOpenFolder(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.FolderBrowserDialog FBD = new System.Windows.Forms.FolderBrowserDialog();
			if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				ImageFolderPath = FBD.SelectedPath;
				DirectoryInfo ImageFolderInfo = new DirectoryInfo(ImageFolderPath);
				Images = ImageFolderInfo.GetFiles()
					.Where(file => ".jpg .bmp .jpeg .png .gif".Contains(file.Extension))
					.Select(x => x)
					.ToArray();
				if (Images.Length > 0)
				{
					CurrentImageIndex = 0;
					Image.Source = new BitmapImage(new Uri(Images[CurrentImageIndex].FullName));
				}
			}

		}

		private void OnPrev(object sender, RoutedEventArgs e)
		{
			if (Images != null && Images.Length > 0)
			{
				CurrentImageIndex = CurrentImageIndex > 0 ? CurrentImageIndex - 1 : Images.Length - 1;
				Image.Source = new BitmapImage(new Uri(Images[CurrentImageIndex].FullName));
			}
		}

		private void OnNext(object sender, RoutedEventArgs e)
		{
			if (Images != null && Images.Length > 0)
			{
				CurrentImageIndex = CurrentImageIndex < Images.Length - 1 ? CurrentImageIndex + 1 : 0;
				Image.Source = new BitmapImage(new Uri(Images[CurrentImageIndex].FullName));
			}
		}

		private void OnComPortSelect(object sender, EventArgs e)
		{
			PortComboBox.ItemsSource = SerialPort.GetPortNames();
		}

		private void OnComConnect(object sender, RoutedEventArgs e)
		{
			try
			{
				if (Port != null && Port.IsOpen)
				{
					Port.Close();
				}
				Port = new SerialPort((string)PortComboBox.SelectedItem, (int)BaudRateComboBox.SelectedItem);
				Port.RtsEnable = true; //DataReceived not fireing without this
				Port.DataReceived += new SerialDataReceivedEventHandler(ComDataReceivedHandler);
				Port.Open();
			}
			catch
			{
				MessageBox.Show("Com port didn't open", "Error");
			}
		}

		private void ComDataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
		{
			SerialPort port = (SerialPort)sender;
			Dispatcher.BeginInvoke(new Action(() => 
			{
				if (Images != null && Images.Length > 0)
				{
					CurrentImageIndex = CurrentImageIndex < Images.Length - 1 ? CurrentImageIndex + 1 : 0;
					Image.Source = new BitmapImage(new Uri(Images[CurrentImageIndex].FullName));
				}
			}));
			string indata = port.ReadExisting();
			Trace.WriteLine("Data Received:");
			Trace.WriteLine(indata);
		}

		private void OnComDisconnect(object sender, RoutedEventArgs e)
		{
			if (Port != null && Port.IsOpen)
			{
				try
				{
					Port.Close();
				}
				catch { }
			}
		}

		private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (Port != null && Port.IsOpen)
			{
				try
				{
					Port.Close();
				}
				catch { }
			}
		}

		CurrentWindowState LastWindowState;

		private void ToggleFullscreen(object sender, RoutedEventArgs e)
		{
			Fullscreen = !Fullscreen;
			if (Fullscreen)
			{
				LastWindowState = new CurrentWindowState
					{
						WindowStyle = this.WindowStyle,
						ResizeMode = this.ResizeMode,
						Left = this.Left,
						Top = this.Top,
						Width = this.Width,
						Height = this.Height,
						Topmost = this.Topmost
					};
				this.WindowStyle = WindowStyle.None;
				this.ResizeMode = ResizeMode.NoResize;
				this.Left = 0;
				this.Top = 0;
				this.Width = SystemParameters.VirtualScreenWidth;
				this.Height = SystemParameters.VirtualScreenHeight;
				this.Topmost = true;
			}
			else
			{
				this.WindowStyle = LastWindowState.WindowStyle;
				this.ResizeMode = LastWindowState.ResizeMode;
				this.Left = LastWindowState.Left;
				this.Top = LastWindowState.Top;
				this.Width = LastWindowState.Width;
				this.Height = LastWindowState.Height;
				this.Topmost = LastWindowState.Topmost;
			}
		}

		struct CurrentWindowState
		{
			public WindowStyle WindowStyle;
			public ResizeMode ResizeMode;
			public double Left;
			public double Top;
			public double Width;
			public double Height;
			public bool Topmost;
		}
	}
}
