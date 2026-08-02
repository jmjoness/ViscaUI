using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ViscaUI;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsDialog : ContentDialog
{
	static string[] ModeStrs = { "EVI-D70/80/90", "EVI-D30" };

	public SettingsDialog()
	{
		InitializeComponent();

		this.Height = 400;
		this.Title = "Settings";
		this.CloseButtonText = "Done";
		this.Loaded += SettingsDialog_Loaded;
	}

	private void SettingsDialog_Loaded(object sender, RoutedEventArgs e)
	{

		string[] ports = SerialPort.GetPortNames();

		foreach (string s in ports) {
			portCombo.Items.Add(s);
		}

		string p = Config.Port;
		int np = portCombo.Items.IndexOf(p);
		portCombo.SelectedIndex = np;

		speedCombo.Items.Add("9600");
		speedCombo.Items.Add("38400");
		string speed = Config.Speed.ToString();
		int ns = speedCombo.Items.IndexOf(speed);
		speedCombo.SelectedIndex = ns;

		modeCombo.Items.Add("D70");
		modeCombo.Items.Add("D30");
		Mode m = Config.Mode;
		int nm = modeCombo.Items.IndexOf(m.ToString());
		modeCombo.SelectedIndex = nm;

		miniCheck.IsChecked = Config.Mini;
		commsCheck.IsChecked = Config.Debug;
		logCheck.IsChecked = Config.Log;
	}

	private void speedCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (speedCombo.SelectedItem is string s && int.TryParse(s, out int sp)) {
			if (Config.Speed != sp) {
				Config.Speed = sp;
			}
		} else {
			Config.Speed = 9600;
		}
	}

	private void portCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (portCombo.SelectedItem is string s && (Config.Port != s)) {
			Config.Port = s;
		}
	}

	private void modeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (modeCombo.SelectedItem is string s && (ModeStrs[(int)Config.Mode] != s)) {
			Config.Mode = (s == "EVI-D30") ? Mode.D30 : Mode.D70;
		}
	}

	private void miniCheck_Checked(object sender, RoutedEventArgs e) {
		Config.Mini = true;
	}

	private void miniCheck_Unchecked(object sender, RoutedEventArgs e) {
		Config.Mini = false;
	}

	private void commsCheck_Checked(object sender, RoutedEventArgs e) {
		Config.Debug = true;
	}

	private void commsCheck_Unchecked(object sender, RoutedEventArgs e) {
		Config.Debug = false;
	}

	private void logCheck_Checked(object sender, RoutedEventArgs e) {
		Config.Log = true;
	}

	private void logCheck_Unchecked(object sender, RoutedEventArgs e) {
		Config.Log = false;
	}
}
