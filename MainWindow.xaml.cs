using Microsoft.ML.OnnxRuntime;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging; // WinUI 3
using Microsoft.WindowsAppSDK.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using WinRT.Interop;
using static System.Runtime.InteropServices.JavaScript.JSType;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ViscaUI {
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : Window {
		#region Enums and Constants
		enum BalanceType { Auto, Indoor, Outdoor, OnePush, AutoTrace, Manual }
		static BalanceType balanceType = BalanceType.Auto;

		readonly static Dictionary<BalanceType, string> balanceStrMap = new Dictionary<BalanceType, string> {
			{ BalanceType.Auto, "Auto" },
			{ BalanceType.Indoor, "Indoor" },
			{ BalanceType.Outdoor, "Outdoor" },
			{ BalanceType.OnePush, "One Push" },
			{ BalanceType.AutoTrace, "Auto Trace" },
			{ BalanceType.Manual, "Manual" }
		};

		readonly static Dictionary<BalanceType, byte> balanceCmdMap = new Dictionary<BalanceType, byte> {
			{ BalanceType.Auto, 0x00 },
			{ BalanceType.Indoor, 0x01 },
			{ BalanceType.Outdoor, 0x02 },
			{ BalanceType.OnePush, 0x03 },
			{ BalanceType.AutoTrace, 0x04 },
			{ BalanceType.Manual, 0x05 }
		};

		readonly static byte[] CommandByte = [ 0x00, 0x00, 0x00,
										0x00, 0x00, 0x01, 0x03, 0x04, 
										0x07, 0x3F, 
										0x08, 0x38, 0x39, 0x4D, 0x33, 
										0x3E, 0x4E, 0x35, 0x43, 0x44, 
										0x00, 0x57, 0x39, 0x4d, 0x33, 
										0x3F, 0x35, 0x43, 0x44, 0x3E, 
										0x4E, 0x02 ];

		readonly Dictionary<int, string> vendorMap = new Dictionary<int, string> {
			{ 0x01, "Sony" },
			{ 0x03, "Everet" },
			{ 0x10, "Sony" },
			{ 0x20, "Canon" },
			{ 0x30, "Panasonic" },
			{ 0x220, "Datavideo" },
			{ 0x800, "Lumens" },
			{ 0x0F0F, "GNT" },
			{ 0x2574, "AVer" }
		};

		readonly Dictionary<int, string> sonyModelMap = new Dictionary<int, string> {
			{ 0x400, "EVI-D30" },
			{ 0x401, "EVI-D100" },
			{ 0x403, "EVI-D70" },
			{ 0x404, "EVI-D70" },
			{ 0x40D, "EVI-D100" },
			{ 0x40E, "EVI-D70" },
			{ 0x504, "EVI-HD1" },
			{ 0x505, "EVI-HD3" },
			{ 0x507, "EVI-HD7" },
			{ 0x514, "EVI-H100" }
		};
		readonly Dictionary<int, string> averModelMap = new Dictionary<int, string> {
			{ 0x559, "MD330" },
			{ 0x565, "MD120" },
			{ 0x500, "PTZ210" },
			{ 0x510, "PTC500" }
		};

		uint NoWBManual = 0x01;
		uint NoBrightDirect = 0x02;

		//readonly string[] IrisStrings = { " --", "F22", "F19", "F16", "F14", "F11", "F9.6", "F8.0", "F6.8", "F5.6", "F4.8",
		//								 "F4.0", "F3.4", "F2.8", "F2.4", "F2.0", "F1.6", "F1.4", "F1.4", "F1.4", "F1.4",
		//								 "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4",
		//								 "F1.4", "F1.4" };
		//string[] GainStrings = { "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB",
		//								 "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "2 dB", "4 dB",
		//								 "6 dB", "8 dB", "10 dB", "12 dB", "14 dB", "16 dB", "18 dB", "20 dB", "22 dB", "24 dB",
		//								 "26 dB", "28 dB" };
		//string[] IrisD30Strings = { " --", "F28", "F22", "F19", "F16", "F14", "F11", "F9.6", "F8.0", "F6.8", "F5.6", "F4.8",
		//								 "F4.0", "F3.4", "F2.8", "F2.4", "F2.0", "F1.8" };
		//string[] GainD30Strings = { "-3 dB", "0 dB", "3 dB", "6 dB", "9 dB", "12 dB", "15 dB", "18 dB", "21 dB", "24 dB", "27 dB",
		//									 "30 dB", "33 dB", "36 dB", "39 dB", "42 dB", "45 dB" };
		//string[] ExpCompStrings = { "-10.5 dB", "-9 dB", "-7.5 dB", "-6 dB", "-4.5 dB", "-3 dB", "-1.5 dB", "0 dB",
		//									 "1.5 dB", "3 dB", "4.5 dB", "6 dB", "7.5 dB", "9 dB", "10.5 dB" };
		//string[] ShutterStrings = { "1/1", "1/2", "1/4", "1/8", "1/15", "1/30", "1/60", "1/90", "1/100", "1/125",
		//									 "1/180", "1/250", "1/350", "1/500", "1/725", "1/1000", "1/1500", "1/2000", "1/3000", "1/4000",
		//									 "1/6000", "1/10000" };
		#endregion
		#region Class Variables
		private readonly SerialPortService _service = new();


		readonly	static MsgQueue msgQueue = new();
		static CommandType lastCmdType = CommandType.None;
		static DateTime lastSend = DateTime.Now;
		static int lastMsgNum = 0;

		public bool keepReading = false;
		readonly static Queue<byte> receiveQueue = new Queue<byte>();

		static int numDevices = 0;
		static uint deviceId = 1;
		static bool powerOn = false;
		static Mode currentMode = Mode.D70;
		static bool noWBManual = false;
		static List<DeviceInfo> deviceInfos = new List<DeviceInfo>();

		static byte panRate = 1;
		static byte tiltRate = 1;
		static byte zoomRate = 1;
		static byte focusRate = 1;
		static bool ptRectDragging = false;
		static bool zmRectDragging = false;
		static bool focusRectDragging = false;
		static int lastPanRate = 0;
		static int lastTiltRate = 0;
		static int lastZoomRate = 0;
		static int lastFocusRate = 0;

		static bool loggingOn = false;
		static bool debugOn = false;
		static bool loaded = false;

		Microsoft.UI.Windowing.AppWindow? appWindow = null;

		#endregion

		#region UI Elements

		List<Button> presetButtons = new();
		List<TextBox> presetTexts = new();
		List<Panel> presetPanels = new();
		List<RadioButton> deviceButtons = new();
		List<TextBox> cameraTexts = new();
		List<Button> ptzButtons = new();		 
		List<Control> focusControls = new();
		List<Control> exposureControls = new();

		#endregion

		#region Constants
		const int addressBase = 0x80;
		const byte camByte2 = 0x01;
		const byte normCmd = 0x04;
		const byte panTiltCmd = 0x06;

		#endregion

		private static System.Timers.Timer? presetTimer;
		Button? lastPreset = null;
		int lastPresetNumber = 0;
		bool settingPreset = false;

		private static System.Timers.Timer? receiveTimer;

		private static bool connectionFailed = false;
		private static bool messageDialogShowing = false;


		//Color normalColor;

		//int xFactor = 1;
		//int xCenter = 0;
		//int yFactor = 1;
		//int yCenter = 0;
		//int zFactor = 1;
		//int zCenter = 0;

		//bool calibrateMode = false;
		//int calMaxX = 0;
		//int calMinX = 65535;
		//int calMaxY = 0;
		//int calMinY = 65535;
		//int calMaxZ = 0;
		//int calMinZ = 65535;

		//int currentPreset = 0;
		//Color panelBackgroundColor;
		//bool buttonDown = false;
		//bool povDown = false;

		//int LeftPresetBtn = 0;
		//int RightPresetBtn = 2;
		//int SetPresetBtn = 1;
		//int SelectPresetBtn = 3;
		//int TriggerBtn = 7;

		#region Simple Logger
		public static class SimpleLogger {
			static string appDataPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
			private static string LogFilePath = Path.Combine(appDataPath, "Visca-01.log");
			private static bool initialized = false;
			private static bool logExists = false;
			private static Queue<string> waitingMsg = new Queue<string>();

			private static async void Init() {
				try {
					string logFolderPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
					string[] logFiles = Directory.GetFiles(appDataPath, "Visca-*.log");

					int logNo = 1;

					foreach (var file in logFiles) {
						string fileName = Path.GetFileName(file);
						int n = 0;
						string nStr = fileName.Replace("Visca-", "").Replace(".log", "");
						if (Int32.TryParse(nStr, out n)) {
							logNo = int.Max(logNo, n);
						}
					}

					logNo++;
					if (logNo > 10) {
						logNo = 1;
					}

					initialized = true;

					int nextLog = logNo;
					//Config.Instance = nextLog;
					string fName = $"Visca-{nextLog:D2}.log";
					string fPath = Path.Combine(appDataPath, fName);
					Debug.WriteLine($"log path: {fPath}");
					if (File.Exists(fPath)) {
						File.Delete(fPath);
					}

					string logEntry = $"{DateTime.Now:MM-dd} - ViscaUI data log {Environment.NewLine}";
					File.WriteAllText(fPath, logEntry);
					if (File.Exists(fPath)) {
						LogFilePath = fPath;
						logExists = true;
					}
				} catch (System.IO.IOException exc) {
					Debug.WriteLine($"Init() system IO exception: {exc}");
				} catch (Exception exc) {
					Debug.WriteLine($"Init() exception: {exc}");
				}
			}

			public static async Task LogAsync(string message) {
				if (!initialized) {
					Init();
				}

				string logEntry = $"{DateTime.Now:MM-dd HH:mm:ss.fff} - {message}{Environment.NewLine}";

				if (FileLocked()) {
					waitingMsg.Enqueue(logEntry);
				} else {
					if (logExists) {
						while (waitingMsg.Count > 0) {
							string msg = waitingMsg.Dequeue();
							await File.AppendAllTextAsync(LogFilePath, msg);
						}

						await File.AppendAllTextAsync(LogFilePath, logEntry);
					}
				}
			}

			private static bool FileLocked() {
				try {
					using (FileStream stream = new FileStream(LogFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
						stream.Close();
					}
				} catch (IOException) {
					return true;
				}

				return false;
			}
		}
		#endregion

		public void dfo(string txt) {
			if (loggingOn) {
#pragma warning disable CS4014
				SimpleLogger.LogAsync(txt);
#pragma warning restore CS4014
			}

			if (debugOn) {
				responseListAdd(txt);
			}
		}

		public MainWindow() {
			Debug.WriteLine("MainWindow()");
			InitializeComponent();
			Title = $"ViscaUI v{Assembly.GetExecutingAssembly().GetName().Version}";

			AppWindow.SetIcon("camera.ico");

			_service.DataReceived += Service_DataReceived;
			_service.ErrorOccurred += Service_ErrorOccurred;
			_service.ConnectionLost += Service_ConnectionLost;

			init();
		}

		private void ContentLoaded(object sender, RoutedEventArgs e) {
			string result = _service.Connect();

			ShowConnectedState(result == "");
			if (result == "") {
				ShowConnectedState(true);
				responseListAdd("Connected");
				broadcastAddress();
			} else {
				ShowConnectedState(false);
				responseListAdd(result);
			}

			setControlsState();

			loaded = true;
		}

		private void init() {
			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
			appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

			System.Drawing.Point location = Config.Location;
			appWindow.Move(new Windows.Graphics.PointInt32(location.X, location.Y));
			appWindow.Changed += (s, e) => {
				if (e.DidPositionChange) {
					var newPos = s.Position;
					Config.Location = new System.Drawing.Point(newPos.X, newPos.Y);
				}
			};

			appWindow.Resize(new Windows.Graphics.SizeInt32(510, Config.Debug ? 850 : 510));

			loggingOn = Config.Log;
			debugOn = Config.Debug;

			dfo("start");

			deviceButtons.Add(C1);
			deviceButtons.Add(C2);
			deviceButtons.Add(C3);
			deviceButtons.Add(C4);
			deviceButtons.Add(C5);
			deviceButtons.Add(C6);
			deviceButtons.Add(C7);

			presetButtons.Add(p1Btn);
			presetButtons.Add(p2Btn);
			presetButtons.Add(p3Btn);
			presetButtons.Add(p4Btn);
			presetButtons.Add(p5Btn);
			presetButtons.Add(p6Btn);

			presetTexts.Add(p1TextBox);
			presetTexts.Add(p2TextBox);
			presetTexts.Add(p3TextBox);
			presetTexts.Add(p4TextBox);
			presetTexts.Add(p5TextBox);
			presetTexts.Add(p6TextBox);

			presetPanels.Add(p1Panel);
			presetPanels.Add(p2Panel);
			presetPanels.Add(p3Panel);
			presetPanels.Add(p4Panel);
			presetPanels.Add(p5Panel);
			presetPanels.Add(p6Panel);

			foreach (Button b in  presetButtons) {
				b.AddHandler( UIElement.PointerPressedEvent, new PointerEventHandler(PresetDown), handledEventsToo: true );
				b.AddHandler( UIElement.PointerReleasedEvent, new PointerEventHandler(PresetUp), handledEventsToo: true );
			}

			string[] presets = Config.GetPresets(deviceId - 1);
			for (int i = 0; i < presets.Length; i++) {
				presetTexts[i].Text = presets[i];
			}

			C1.IsChecked = true;

			dfo("init");
		}

		private void setControlsState() {
			this.DispatcherQueue.TryEnqueue(() => {
				bool enable = false;
				if ((_service != null) && _service.IsConnected) {
					enable = true;
					//ShowPowerState(true);
				} else {
					enable = false;
					//ShowPowerState(false);
				}

				foreach (Button b in ptzButtons) {
					b.IsEnabled = enable;
				}

				foreach (Button b in presetButtons) {
					b.IsEnabled = enable;
				}

				foreach (TextBox t in presetTexts) {
					t.IsEnabled = enable;
				}

				displayBrightMode(false);
				displayExpComp(false);
				balanceSetup();
			});
		}

		private void ShowPowerState(bool on) {
			powerOn = on;
			this.DispatcherQueue.TryEnqueue(() => {

				string state = on ? "on" : "off";
				powerImg.Source = new SvgImageSource(new Uri($"ms-appx:///Assets/power-{state}.svg"));
			});
		}
		private void ShowConnectedState(bool on) {
			this.DispatcherQueue.TryEnqueue(() => {
				string state = on ? "" : "dis";
				connectImg.Source = new SvgImageSource(new Uri($"ms-appx:///Assets/{state}connected.svg"));
			});
		}

		private async void SettingsClick(object sender, RoutedEventArgs e) {
			responseListAdd("settings");
			bool debug = Config.Debug;
			var dialog = new SettingsDialog();
			dialog.XamlRoot = this.Content.XamlRoot; // Required for WinUI 3
			await dialog.ShowAsync();

			Debug.WriteLine("after settings");
			if (debug != Config.Debug) {
				if (appWindow is not null) {
					appWindow.Resize(new Windows.Graphics.SizeInt32(510, Config.Debug ? 850 : 510));
				}
			}
		}

		private void ConnectClick(object sender, RoutedEventArgs e) {
			responseListAdd("ConnectClick()");
			if (_service.IsConnected) {
				responseListAdd("disconnect");
				_service.Disconnect();
				ShowConnectedState(false);
			} else {
				try {
					responseListAdd("connect");
					_service.Connect();
					ShowConnectedState(true);
					broadcastAddress();
				} catch (Exception exc) {
					responseListAdd("Exception connecting: " + exc.Message);
				}
			}

			setControlsState();
		}

		private void PowerClick(object sender, RoutedEventArgs e) {
			if (powerOn) {
				byte[] d = { 0x03 };
				sendCommand(CommandType.CMD_Power, d, "off");
				ShowPowerState(false);
			} else {
				byte[] d = { 0x02 };
				sendCommand(CommandType.CMD_Power, d, "on");
				ShowPowerState(true);
				setControlsState();
			}
		}

		#region Send
		private string dataToHex(byte[] data) {
			string msgStr = "";
			foreach (byte b in data) {
				msgStr += b.ToString("X2") + " ";
			}

			return msgStr;
		}

		private void SendMsg(VMessage msg) {
			if (_service.IsConnected) {
				lastMsgNum++;
				msg.msgNum = lastMsgNum;

				if (lastCmdType == CommandType.None) {
					ProcessSendMsg(msg);
				} else {
					dfo($"QUE: {msg.cmdType.ToString().PadRight(25)} - {dataToHex(msg.data)}");
					msgQueue.Enqueue(msg);
				}
			} else if (!connectionFailed) {
				connectionFailed = true;
				dfo("port is not connected");
				_service.Disconnect();
				msgQueue.Clear();
				lastCmdType = CommandType.None;
				ShowConnectedState(false);
				setControlsState();

				ShowMessage("Port disconnected ");
			}
		}

		private void ProcessSendMsg(VMessage msg) {
			lastCmdType = msg.cmdType;
			List<byte> msgData = new();

			switch (msg.msgType) {
				case MessageType.MSG_Broadcast:
					msgData.Add(0x88);
					break;
				case MessageType.MSG_Command:
					msgData.Add(getAddressByte());
					msgData.Add(0x01);
					msgData.Add((msg.cmdType == CommandType.CMD_PanTilt) ? panTiltCmd : normCmd);
					msgData.Add((byte)CommandByte[(int)msg.cmdType]);
					break;
				case MessageType.MSG_Inquiry:
					msgData.Add(getAddressByte());
					msgData.Add(0x09);
					msgData.Add((msg.cmdType == CommandType.INQ_DeviceType) ? (byte)0x00 : (byte)0x04);
					msgData.Add((byte)CommandByte[(int)msg.cmdType]);
					break;
				default:
					dfo("Unknown message type: " + msg.msgType.ToString());
					return;
			}

			msgData.AddRange(msg.data);
			msgData.Add(0xFF);
			byte[] ary = msgData.ToArray();
			_service.Write(ary);

			string info = msg.cmdType.ToString() + (msg.comment.Length > 0 ? (", " + msg.comment) : "");
			dfo($"SND: {info.PadRight(25)} - {dataToHex(ary)}");
			lastSend = DateTime.Now;
			receiveTimer = new System.Timers.Timer(3000);
			receiveTimer.Elapsed += ReceiveTimer_Tick;
			receiveTimer.Enabled = true;
		}

		private  byte getAddressByte() {
			return (byte)(addressBase | deviceId);
		}
		private void sendCommand(CommandType cmdType, byte[] data, string more = "") {
			VMessage msg = new VMessage(MessageType.MSG_Command, cmdType, getAddressByte(), data, more);
			SendMsg(msg);
		}

		//private void sendInquiry(MsgType type) {
		private void sendInquiry(CommandType cmdType, string more = "") {
			VMessage msg = new VMessage(MessageType.MSG_Inquiry, cmdType, getAddressByte(), new byte[0], more);
			SendMsg(msg);
		}

		private  void sendDeviceTypeInquiry() {
			sendInquiry(CommandType.INQ_DeviceType, "Device Type Inquiry");
		}
		private  void broadcastAddress() {
			VMessage msg = new VMessage(MessageType.MSG_Broadcast, CommandType.BDC_AddressSet, 0x88, new byte[] { 0x30, 0x01 });
			SendMsg(msg);
		}
		#endregion

		#region Receive
		private  void Service_DataReceived(object? sender, byte[] data) {
			if (receiveTimer is not null) {
				receiveTimer.Stop();
				receiveTimer.Dispose();
			}
			foreach (byte b in data) {
				receiveQueue.Enqueue(b);
			}
			while (receiveQueue.Contains(0xFF)) {
				List<byte> response = new();
				do {
					response.Add(receiveQueue.Dequeue());
				} while (response[response.Count - 1] != 0xFF);
				string str = "";
				foreach (byte b in response) {
					str += b.ToString("X2") + " ";
				}
				string receiveString = "";
				if (response.Count == 3) {
					receiveString = handle3ByteResponse(response);
				} else if (response.Count == 4) {
					receiveString = "RCV: " + handle4ByteResponse(response);
				} else if (response.Count == 7) {
					receiveString = "RCV: " + handle7ByteResponse(response);
				} else if (response.Count == 10) {
					receiveString = "RCV: " + handle10ByteResponse(response);
				} else {
					receiveString = "Unknown";
				}
				if (receiveString.Length > 0) {
					dfo($"{receiveString.PadRight(30)} - {str}");
				}
			}

			if (lastCmdType == CommandType.None) {
				if (!msgQueue.Empty) {
					VMessage msg = new VMessage();
					msgQueue.Dequeue(ref msg);
					SendMsg(msg);
				}
			}
		}

		private void ReceiveTimer_Tick(object? sender, object e) {
			if (receiveTimer is not null) {
				receiveTimer.Stop();
				receiveTimer.Dispose();
			}
			_service.Disconnect();
			msgQueue.Clear();
			lastCmdType = CommandType.None;
			ShowConnectedState(false);
			setControlsState();
			ShowMessage("Port closed - no response from " + lastCmdType.ToString());
		}

		private async void ShowMessage(string msg) {
			if (messageDialogShowing) {
				responseListAdd("message dialog already showing");
				return;
			}
		
			messageDialogShowing = true;
			responseListAdd("Show: " + msg);
			try {
				this.DispatcherQueue.TryEnqueue(() => {
					ContentDialog dialog = new ContentDialog {
						Title = "ViscaUI Message",
						Content = msg,
						CloseButtonText = "OK",
						XamlRoot = ContentFrame.XamlRoot // Critical requirement
					};

					dialog.Closed += ContentDialog_Closed;
					_ = dialog.ShowAsync();
				});
			} catch (Exception exc) {
				responseListAdd($"show message exception: {exc}");
			}
		}

		private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args) {
			responseListAdd("Dialog closed");
			messageDialogShowing = false;
		}

		private  void Service_ErrorOccurred(object? sender, string message) {
			responseListAdd("Service error occurred");
			ShowMessage("Service error occurred");
		}

		private  async void Service_ConnectionLost(object? sender, EventArgs e) {
			responseListAdd("Connection Lost");
			ShowMessage("Connection Lost");
		}

		private  string handle3ByteResponse(List<byte> response) {
			string rtn = "";
			int dev = ((int)((response[0] >> 4) & 0x7));
			if (response[1] == 0x38) {
				rtn = $"CHG: Device {dev}";
				broadcastAddress();
			} else if ((response[1] & 0xF0) == 0x40) {
				rtn = $"ACK: Device {dev}";
			} else if ((response[1] & 0xF0) == 0x50) {
				rtn = $"FIN: Device {dev}";
				lastCmdType = CommandType.None;
			}

			return rtn;
		}

		private  string handle4ByteResponse(List<byte> response) {
			string rtn = "";
			try {
				if ((response[0] == 0x88) && (response[1] == 0x30)) {
					rtn = "Address Set Return";
					numDevices = (int)(response[2]) - 1;
					lastCmdType = CommandType.None;
					ShowValidCameras(numDevices);
					sendInquiry(CommandType.INQ_DeviceType);
				} else if ((response[0] & 0x8F) == 0x80) {
					if ((response[1] & 0xF0) == 0x60) {    // error message
						Debug.WriteLine("Error message received");
						switch (response[2]) {
							case 0x41:
								rtn = "invalid command: ";
								foreach (byte b in response) {
									rtn += b.ToString("X2");
								}
								lastCmdType = CommandType.None;
								break;
							case 0x02:
								rtn = "invalid parameters or format command: ";
								foreach (byte b in response) {
									rtn += b.ToString("X2");
								}
								lastCmdType = CommandType.None;
								break;
							default:
								rtn = "command error: ";
								foreach (byte b in response) {
									rtn += b.ToString("X2");
								}
								lastCmdType = CommandType.None;
								break;
						}
					} else if (response[1] == 0x50) {  // inquiry response
						switch (lastCmdType) {
							case CommandType.INQ_Power:
								if (response[2] == 0x02) {
									ShowPowerState(true);
									rtn = "Power On";
									sendInquiry(CommandType.INQ_AEMode);
									sendInquiry(CommandType.INQ_FocusMode);
									sendInquiry(CommandType.INQ_BalanceMode);
								} else if (response[2] == 0x03) {
									ShowPowerState(false);
									rtn = "Power Off";
								}
								lastCmdType = CommandType.None;
								setControlsState();
								break;
							case CommandType.INQ_FocusMode:
								if (response[2] == 0x02) {
									ShowFocusState(true);
									rtn = "Focus Auto";
								} else if (response[2] == 0x03) {
									ShowFocusState(false);
									rtn = "Focus Manual";
								}
								lastCmdType = CommandType.None;
								break;
							case CommandType.INQ_AEMode:
								if (response[2] == 0x00) {
									setBrightType(false);
									rtn = "Exposure Auto ";
								} else if (response[2] == 0x0D) {
									setBrightType(true);
									rtn = "Exposure Bright";
								}
								lastCmdType = CommandType.None;
								break;
							case CommandType.INQ_BacklightMode:
								if (response[2] == 0x02) {
									ShowBacklit(true);
									rtn = "Backlight On";
								} else if (response[2] == 0x03) {
									ShowBacklit(false);
									rtn = "Backlight Off";
								}
								lastCmdType = CommandType.None;
								break;
							case CommandType.INQ_BalanceMode:
								BalanceType bal = BalanceType.Auto;
								switch (response[2]) {
									case 0:
										bal = BalanceType.Auto;
										break;
									case 1:
										bal = BalanceType.Indoor;
										break;
									case 2:
										bal = BalanceType.Outdoor;
										break;
									case 5:
										if (currentMode == Mode.D70) {
											bal = BalanceType.Manual;
										}
										break;
								}
								rtn = "Balance Mode: " + bal.ToString();
								lastCmdType = CommandType.None;
								break;
							case CommandType.INQ_ExpCompOn:
								Debug.WriteLine("exp comp return");
								bool on = false;
								switch (response[2]) {
									case 2:
										on = true;
										break;
									case 3:
										on = false;
										break;
								}
								rtn = "Exp Comp: " + (on ? "On" : "Off");
								displayExpComp(on);
								lastCmdType = CommandType.None;
								sendInquiry(CommandType.INQ_ExpCompPos);
								break;
							}
						}
					}

			} catch (COMException exc) {
				Debug.WriteLine("COMException in handle4ByteResponse: " + exc.Message);
				return rtn;
			} catch (Exception exc) {
				Debug.WriteLine("Exception in handle4ByteResponse: " + exc.Message);
				return rtn;
			}

			return rtn;
		}

		private  string handle7ByteResponse(List<byte> response) {
			string rtn = "";
			if (response[1] == 0x50) {  // inquiry response
				if (lastCmdType == CommandType.INQ_BrightPos) {
					int pos = ((response[4] << 4) | response[5]);
					ShowExposure(pos);
					rtn = $"Bright {pos}";
					lastCmdType = CommandType.None;
					//irisLabel.Text = IrisStrings[pos];
					//gainLabel.Text = GainStrings[pos];
				} else if (lastCmdType == CommandType.INQ_BalanceRed) {
					int pos = ((response[4] << 4) | response[5]);
					rtn = $"Red balance {pos}";
					lastCmdType = CommandType.None;
					//redLabel.Text = pos.ToString();
				} else if (lastCmdType == CommandType.INQ_BalanceBlue) {
					int pos = ((response[4] << 4) | response[5]);
					rtn = $"Blue balance {pos}";
					lastCmdType = CommandType.None;
					//blueLabel.Text = pos.ToString();
				} else if (lastCmdType == CommandType.INQ_ExpCompPos) {
					int pos = ((response[4] << 4) | response[5]);
					ShowExposureComp(pos);
					rtn = $"Exp Comp {pos}";
					lastCmdType = CommandType.None;
					//expCompLabel.Text = ExpCompStrings[pos];
				//} else if (lastCmdType == CommandType.INQ_Shutter) {
				//	int pos = ((response[4] << 4) | response[5]);
				//	rtn = $"Shutter Speed {pos}";
				//	lastCmdType = CommandType.None;
				//	//shutterLabel.Text = ShutterStrings[pos];
				//} else if (lastCmdType == CommandType.INQ_Iris) {
				//	int pos = ((response[4] << 4) | response[5]);
				//	rtn = $"Iris {pos}";
				//	lastCmdType = CommandType.None;
				//	//irisLabel.Text = IrisD30Strings[pos];
				//} else if (lastCmdType == CommandType.INQ_Gain) {
				//	int pos = ((response[4] << 4) | response[5]);
				//	rtn = $"Gain {pos}";
				//	lastCmdType = CommandType.None;
				//	//gainLabel.Text = GainD30Strings[pos];
				//	//}
				}
			}

			return rtn;
		}

		private  string handle10ByteResponse(List<byte> response) {
			string rtn = "";
			switch (lastCmdType) {
				case CommandType.INQ_DeviceType:
					byte dev = (byte)((response[0] >> 4) & 0x7);
					int vendorId = ((int)response[2] << 8) + response[3];
					int modelId = ((int)response[4] << 8) + response[5];
					int version = ((int)response[6] << 8) + response[7];
					string vendorStr = vendorMap.ContainsKey(vendorId) ? vendorMap[vendorId] : "Unknown";
					string modelStr = "Unknown";
					if (vendorStr == "Sony") {
						modelStr = sonyModelMap.ContainsKey(modelId) ? sonyModelMap[modelId] : "Unknown";
					} else if (vendorStr == "AVer") {
						modelStr = averModelMap.ContainsKey(modelId) ? averModelMap[modelId] : "Unknown";
					}
					string versionStr = version.ToString("X2");
					uint restrict = 0;
					if (modelStr  == "EVI-D30") {
						restrict |= NoWBManual;
						restrict |= NoBrightDirect;
					}
					DeviceInfo devInfo = new DeviceInfo(dev, "", vendorStr, modelStr, versionStr, restrict);
					deviceInfos.Add(devInfo);
					string cameraStr = " " + vendorStr + "/" + modelStr;
					rtn = "Cam: " + dev.ToString() + cameraStr;
					if (dev == 1) {
						this.DispatcherQueue.TryEnqueue(() => {
							vendorModel.Text = cameraStr;
						});
					}
					lastCmdType = CommandType.None;
					if (dev < numDevices) {
						deviceId++;
						sendInquiry(CommandType.INQ_DeviceType);
					} else {
						deviceId = 1;
						sendInquiry(CommandType.INQ_Power);
					}
					break;
			}
			//	int dev = response[0] & 0x7;
			//int model = ((int)response[4]) << 8 + response[5];
			//rtn = "Device " + dev.ToString() + " Type: " + model.ToString("X2");

			return rtn;
		}

		private  void responseListAdd(string text) {
			this.DispatcherQueue.TryEnqueue(() => {
				responseListBox.Items.Insert(0, text);
				//dfo(text);
			});
		}
		#endregion

		#region Pan Tilt
		private  void panTiltStop() {
			byte[] d = { panRate, tiltRate, 0x03, 0x03 };
			sendCommand(CommandType.CMD_PanTilt, d, "stop");
		}

		private  void panTiltStart(byte b6, byte b7) {
			byte[] d = { panRate, tiltRate, b6, b7 };
			sendCommand(CommandType.CMD_PanTilt, d, $"p {panRate}, t {tiltRate}");
		}

		private  void CenterBtnClick(object sender, RoutedEventArgs e) {
			byte[] d = { };
			sendCommand(CommandType.CMD_PanTiltHome, d, "center");
		}

		private void ptRect_MouseDown(object sender, PointerRoutedEventArgs e) {
			ptRectDragging = true;
			ptRect_MouseMove(sender, e);
		}

		private void ptRect_MouseUp(object sender, PointerRoutedEventArgs e) {
			ptRectDragging = false;
			panTiltStop();
			lastPanRate = 0;
			lastTiltRate = 0;
		}

		const int ptRectWidth = 140;
		const int ptInc = 10;
		const int ptRectHeight = 120;
		private static readonly int[] panRates = new[] { 0, 1, 3, 6, 10, 15, 24 };
		private static readonly int[] tiltRates = new[] { 0, 1, 3, 6, 10, 1 };

		private void ptRect_MouseMove(object sender, PointerRoutedEventArgs e) {
			if (ptRectDragging) {
				PointerPoint ptrPt = e.GetCurrentPoint(ptRect);
				Point pos = ptrPt.Position;
				double x = Math.Max(Math.Min(pos.X, ptRectWidth), 0) - (ptRectWidth / 2);
				double y = Math.Max(Math.Min(pos.Y, ptRectHeight), 0) - (ptRectHeight / 2);
				double ax = Math.Abs(x);
				double ay = Math.Abs(y);
				int pr = 0;
				int tr = 0;

				int xInd = Math.Min((int)(ax / ptInc), 6);
				int yInd = Math.Min((int)(ay / ptInc), 5);
				pr = panRates[xInd];
				tr = tiltRates[yInd];

				panRate = (byte)pr;
				tiltRate = (byte)tr;

				bool change = false;
				if (lastPanRate != pr) {
					lastPanRate = pr;
					change = true;
				}
				if (lastTiltRate != tr) {
					lastTiltRate = tr;
					change = true;
				}

				if (change) {
					if ((tr == 0) && (pr == 0)) {
						panTiltStop();
					} else {
						byte lr = (byte)((pr == 0) ? 3 : ((x < 0) ? 1 : 2));
						byte ud = (byte)((tr == 0) ? 3 : ((y < 0) ? 1 : 2));
						panTiltStart(lr, ud);
					}
					if (lastPresetNumber != -1) {
						this.DispatcherQueue.TryEnqueue(() => {
							presetPanels[lastPresetNumber].Background = new SolidColorBrush(Colors.Transparent);
							lastPresetNumber = -1;
						});
					}
				}
			}
		}
		#endregion

		#region Zooom
		private void zoomStop() {
			responseListAdd("zoom stop");
			byte[] d = { 0x00 };
			sendCommand(CommandType.CMD_Zoom, d, "stop");
		}

		private void zoomIn() {
			byte cmd = (byte)(0x20 | zoomRate);
			byte[] d = { cmd };
			sendCommand(CommandType.CMD_Zoom, d, $"in {zoomRate}");
		}

		private void zoomOut() {
			byte cmd = (byte)(0x30 | zoomRate);
			byte[] d = { cmd };
			sendCommand(CommandType.CMD_Zoom, d, $"out {zoomRate}");
		}

		private void zmRect_MouseDown(object sender, PointerRoutedEventArgs e) {
			zmRectDragging = true;
			zmRect_MouseMove(sender, e);
		}

		private void zmRect_MouseUp(object sender, PointerRoutedEventArgs e) {
			zmRectDragging = false;
			zoomStop();
			lastZoomRate = 0;
		}

		const int zoomInc = 12;
		const int zoomRectHeight = 120;
		private static readonly int[] zoomRates = new[] { 0, 1, 2, 4, 7 };

		private void zmRect_MouseMove(object sender, PointerRoutedEventArgs e) {
			if (zmRectDragging) {
				PointerPoint ptrPt = e.GetCurrentPoint(zmRect);
				Point pos = ptrPt.Position;
				double y = Math.Max(Math.Min(pos.Y, zoomRectHeight), 0) - (zoomRectHeight / 2);
				double ay = Math.Abs(y);
				int zr = 0;

				int yInd = Math.Min((int)(ay / zoomInc), 4);
				zr = zoomRates[yInd];

				zoomRate = (byte)zr;

				bool change = false;
				if (lastZoomRate != zr) {
					lastZoomRate = zr;
					change = true;
				}

				if (change) {
					if (zr == 0) {
						zoomStop();
					} else {
						if (y < 0) {
							zoomIn();
						} else {
							zoomOut();
						};
					}
					if (lastPresetNumber != -1) {
						this.DispatcherQueue.TryEnqueue(() => {
							presetPanels[lastPresetNumber].Background = new SolidColorBrush(Colors.Transparent);
							lastPresetNumber = -1;
						});
					}
				}
			}

		}
		#endregion

		#region Focus
		private void ShowFocusState(bool auto) {
			this.DispatcherQueue.TryEnqueue(() => {
				focusManual.IsChecked = !auto;
				focusRect.Visibility = !auto ? Visibility.Visible : Visibility.Collapsed;
			});
		}

		private void setFocusType(bool manual) {
			focusRect.Visibility = manual ? Visibility.Visible : Visibility.Collapsed;
			if (lastCmdType == CommandType.None) {
				byte[] d = { 0x38, (byte)(manual ? 0x03 : 0x02) };
				sendCommand(CommandType.CMD_FocusMode, d, $"{(manual ? "manual" : "auto")}");
			}
		}

		private void focusStop() {
			byte[] d = { 0x00 };
			sendCommand(CommandType.CMD_Focus, d, "stop");
		}

		private void focusIn() {
			byte cmd = (byte)(0x30 | focusRate);
			byte[] d = { cmd };
			sendCommand(CommandType.CMD_Focus, d, $"in {focusRate}");
		}

		private void focusOut() {
			byte cmd = (byte)(0x20 | focusRate);
			byte[] d = { cmd };
			sendCommand(CommandType.CMD_Focus, d	, $"out {focusRate}");
		}
		private void FocusManualClick(object sender, RoutedEventArgs e) {
			setFocusType(focusManual.IsChecked == true);
		}

		private void focusRect_MouseDown(object sender, PointerRoutedEventArgs e) {
			focusRectDragging = true;
			focusRect_MouseMove(sender, e);
		}
		private void focusRect_MouseUp(object sender, PointerRoutedEventArgs e) {
			focusRectDragging = false;
			focusStop();
			lastFocusRate = 0;
		}

		const int focusInc = 9;
		const int focusRectHeight = 90;
		private static readonly int[] focusRates = new[] { 0, 1, 2, 4, 7 };

		private void focusRect_MouseMove(object sender, PointerRoutedEventArgs e) {
			if (focusRectDragging) {
				PointerPoint ptrPt = e.GetCurrentPoint(focusRect);
				Point pos = ptrPt.Position;
				double y = Math.Max(Math.Min(pos.Y, focusRectHeight), 0) - (focusRectHeight / 2);
				double ay = Math.Abs(y);
				int fr = 0;

				int yInd = Math.Min((int)(ay / focusInc), 4);
				fr = focusRates[yInd];

				if (ay >= 10) {
					fr = (int)((Math.Log10(ay) - 1.0) * 6);
				}

				focusRate = (byte)fr;

				bool change = false;
				if (lastFocusRate != fr) {
					lastFocusRate = fr;
					change = true;
				}

				if (change) {
					if (fr == 0) {
						focusStop();
					} else {
						if (y < 0) {
							focusIn();
						} else {
							focusOut();
						}
					}
				}
			}
		}

		#endregion

		#region Camera
		private void CameraChecked(object sender, RoutedEventArgs e) {
			RadioButton? rb = sender as RadioButton;
			if (rb != null && rb.IsChecked == true) {
				string? str = rb.Content.ToString();
				deviceId = (str is not null) ? uint.Parse(str) : 1;
				if (deviceId <= deviceInfos.Count) {
					string[] presets = Config.GetPresets(deviceId - 1);
					for (int i = 0; i < presets.Length; i++) {
						presetTexts[i].Text = presets[i];
					}
					DeviceInfo info = deviceInfos[(int)deviceId - 1];
					string cameraStr = " " + info.vendor + "/" + info.model;
					vendorModel.Text = cameraStr;

					sendInquiry(CommandType.INQ_Power);
				}
			}
		}

		private void ShowValidCameras(int n) {
			this.DispatcherQueue.TryEnqueue(() => {
				for (int i = 0; i < 7; i++) {
					deviceButtons[i].Visibility = (i < numDevices ? Visibility.Visible : Visibility.Collapsed);
				}
			});
		}
		#endregion

		#region Presets
		private void PresetDown(object sender, PointerRoutedEventArgs e) {
			lastPreset = (Button)sender;
			string? lpStr = lastPreset.Content.ToString();
			lastPresetNumber = (lpStr is not null) ? int.Parse(lpStr) - 1 : 0;
			settingPreset = false;
			presetTimer = new System.Timers.Timer(1000);
			presetTimer.Elapsed += PresetTimer_Tick;
			presetTimer.Enabled = true;
		}

		private void PresetUp(object sender, PointerRoutedEventArgs e) {
			if (presetTimer is not null) {
				presetTimer.Stop();
				presetTimer.Dispose();
			}
			lastPreset = null;
			lastPresetNumber = -1;
			string? btnStr = ((Button)sender).Content.ToString();
			if (btnStr is not null) {
				int btnNbr = int.Parse(btnStr) - 1;
				handlePreset((byte)(btnNbr), presetTimer);
			}
		}

		private void PresetTimer_Tick(object? sender, object e) {
			settingPreset = true;
			this.DispatcherQueue.TryEnqueue(() => {
				if (lastPreset is not null) {
					this.DispatcherQueue.TryEnqueue(() => {
						presetPanels[lastPresetNumber].Background = new SolidColorBrush(Colors.Red);
					});
				}
			});
			if (presetTimer is not null) {
				presetTimer.Stop();
				presetTimer.Dispose();
			}
		}

		private void handlePreset(byte number, System.Timers.Timer? presetTimer1) {
			this.DispatcherQueue.TryEnqueue(() => {
				for (int i = 0; i < 6; i++) {
					presetPanels[i].Background = new SolidColorBrush(Colors.Transparent);
				}
				presetButtons[number].Background = new SolidColorBrush(Colors.Azure);
				presetPanels[number].Background = new SolidColorBrush(Colors.Maroon);
			});

			byte[] d = { 0x01, number };
			d[0] = settingPreset ? (byte)0x01 : (byte)0x02;
			sendCommand(CommandType.CMD_Memory, d, $"preset {number + 1}");

			if (presetTimer1 is not null) {
				presetTimer1.Enabled = false;
			}
			lastPreset = null;
			lastPresetNumber = number;

			if (!settingPreset) {
				sendInquiry(CommandType.INQ_AEMode);
				sendInquiry(CommandType.INQ_FocusMode);
				sendInquiry(CommandType.INQ_BalanceMode);
				sendInquiry(CommandType.INQ_ExpCompOn);
			}

			settingPreset = false;
		}

		private void PresetTextChanged(object sender, RoutedEventArgs e) {
			string[] texts = new string[presetTexts.Count];
			for (int i = 0; i < presetTexts.Count; i++) {
				texts[i] = presetTexts[i].Text;
			}

			Config.SetPresets(deviceId - 1, texts);
		}
		#endregion

		#region Exposure
		private void ExpBrightClick(object sender, RoutedEventArgs e) {
			setBrightType(expBrightChk.IsChecked == true);
		}

		private void displayBrightMode(bool manual) {
			this.DispatcherQueue.TryEnqueue(() => {
				expBrightChk.IsChecked = manual;
				expSlider.IsEnabled = manual;
				//brightBtn.Enabled = manual;
				//darkBtn.Enabled = manual;
				expBacklitChk.IsEnabled = !manual;
			});
		}

		private void ShowExposure(int pos) {
			this.DispatcherQueue.TryEnqueue(() => {
				expSlider.Value = pos;
				//expText.Text = $"{IrisStrings[pos]}-{GainStrings[pos]}";
			});
		}

		private void ShowBacklit(bool backlit) {
			this.DispatcherQueue.TryEnqueue(() => {
				expBacklitChk.IsChecked = backlit;
			});
		}

		private void setBrightType(bool manual) {
			displayBrightMode(manual);

			if (manual) {
				setExpComp(false);
			} else {
				ShowExposure(0);
			}

			byte[] d = { (byte)(manual ? 0x0D : 0x00) };
			sendCommand(CommandType.CMD_ExposureMode, d, $"{(manual ? "Manual" : "Auto")}");

			if (!manual) {
				sendInquiry(CommandType.INQ_BacklightMode);
			} else {
				sendInquiry(CommandType.INQ_BrightPos);
			}
		}

		private void ExpSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			if (lastCmdType == CommandType.None) {
				int value = (int)e.NewValue;
				byte p = (byte)((value >> 4) & 1);
				byte q = (byte)(value & 0x0F);
				byte[] d = { 0x00, 0x00, p, q };
				sendCommand(CommandType.CMD_ExposurePos, d, $"{value}");
				ShowExposure(value);
			}
		}

		private void ExpBacklitClick(object sender, RoutedEventArgs e) {
			bool? chk = expBacklitChk.IsChecked;
			if (chk.HasValue) {
				setBacklight(chk.Value);
			}
		}

		private void setBacklight(bool on) {
			if (lastCmdType == CommandType.None) {
				byte[] d = { (byte)(on ? 0x02 : 0x03), 0xFF };
				sendCommand(CommandType.CMD_Backlight, d, $"{(on ? "on" : "off")}");
			}
		}

		private void ExpCompClick(object sender, RoutedEventArgs e) {
			bool? chk = expCompChk.IsChecked;
			if (chk.HasValue) {
				setExpComp(chk.Value);
			}
		}

		private void setExpComp(bool on) {
			displayExpComp(on);

			byte[] d = { 0x3E, (byte)(on ? 0x02 : 0x03) };
			sendCommand(CommandType.CMD_ExpCompOn, d, $"{(on ? "on" : "off")}");

			if (on) {
				sendInquiry(CommandType.INQ_ExpCompPos);
			}
		}

		private void displayExpComp(bool on) {
			this.DispatcherQueue.TryEnqueue(() => {
				expCompChk.IsChecked = on;
				expCompSlider.IsEnabled = on;
			});
		}

		private void ShowExposureComp(int pos) {
			this.DispatcherQueue.TryEnqueue(() => {
				expCompSlider.Value = pos;
				//expCompText.Text = ExpCompStrings[pos];
			});
		}

		private void ExpCompSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			int value = (int)e.NewValue;
			byte p = 0;
			byte q = (byte)(value & 0x0f);
			byte[] d = { 0x4E, 0x00, 0x00, p, q };
			sendCommand(CommandType.CMD_ExpCompPos, d, $"{value}");
			ShowExposureComp(value);
		}
		#endregion

		#region White Balance
		private void balanceSetup() {
			wbSelectCombo.Items.Clear();
			wbSelectCombo.Items.Add(balanceStrMap[BalanceType.Auto]);
			wbSelectCombo.Items.Add(balanceStrMap[BalanceType.Indoor]);
			wbSelectCombo.Items.Add(balanceStrMap[BalanceType.Outdoor]);
			if (deviceId <= deviceInfos.Count) {
				DeviceInfo info = deviceInfos[(int)deviceId - 1];
				uint restrict = deviceInfos[(int)deviceId - 1].restrict;
				bool manualOk = ((restrict & NoWBManual) == 0);
				if (manualOk) {
					wbSelectCombo.Items.Add(balanceStrMap[BalanceType.Manual]);
				}

				wbSelectCombo.SelectedIndex = 0;
			}
		}

		private void setBalanceType(BalanceType balance) {
			balanceType = balance;

			if (wbSelectCombo.Items.Count > 0) {
				//wbSelectCombo.SelectedIndex = (int)balance;
				bool manual = ((BalanceType)balance == BalanceType.Manual);
				wbRedSlider.IsEnabled = manual;
				wbBlueSlider.IsEnabled = manual;

				//byte[] d = { BalanceCmd[(int)balance] };
				//sendCommand(CommandType.CMD_BalanceMode, d, $"{BalanceStrs[(int)balance]}");

				if (manual) {
					sendInquiry(CommandType.INQ_BalanceRed);
					sendInquiry(CommandType.INQ_BalanceBlue);
				}
			}
		}

		private void ShowGainText(TextBox text, int value) {
			this.DispatcherQueue.TryEnqueue(() => {
				if (text != null) {
					text.Text = value.ToString();
				}
			});
		}

		private void BalRedSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			if (loaded) {
				int value = (int)e.NewValue;

				byte p = (byte)(value >> 4);
				byte q = (byte)(value & 0x0f);
				byte[] d = { 0x00, 0x00, p, q };
				sendCommand(CommandType.CMD_BalanceRed, d, $"{value}");
				ShowGainText(wbRedText, value);
			}
		}

		private void BalBlueSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			if (loaded) {
				int value = (int)e.NewValue;
				byte p = (byte)(value >> 4);
				byte q = (byte)(value & 0x0f);
				byte[] d = { 0x00, 0x00, p, q };
				sendCommand(CommandType.CMD_BalanceBlue, d, $"{value}");
				ShowGainText(wbBlueText, value);
			}
		}

		private void BalSelectChanged(object sender, SelectionChangedEventArgs e) {
			if (loaded && powerOn) {
				BalanceType typ = BalanceType.Auto;
				string? bal = wbSelectCombo.SelectedItem as string;
				foreach (KeyValuePair<BalanceType, string> kvp in balanceStrMap) {
					if (kvp.Value == bal) {
						typ = kvp.Key;
					}
				}

				setBalanceType(typ);
				byte[] d = { balanceCmdMap[typ] };
				sendCommand(CommandType.CMD_BalanceMode, d, $"{balanceStrMap[typ]}");
			}
		}

		#endregion
	}
}
