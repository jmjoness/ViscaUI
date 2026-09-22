using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging; // WinUI 3
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Windows.Foundation;
using WinRT.Interop;

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

		readonly static Dictionary<BalanceType, string> balanceStrMap = new() {
			{ BalanceType.Auto, "Auto" },
			{ BalanceType.Indoor, "Indoor" },
			{ BalanceType.Outdoor, "Outdoor" },
			{ BalanceType.OnePush, "One Push" },
			{ BalanceType.AutoTrace, "Auto Trace" },
			{ BalanceType.Manual, "Manual" }
		};

		readonly static Dictionary<BalanceType, byte> balanceCmdMap = new() {
			{ BalanceType.Auto, 0x00 },
			{ BalanceType.Indoor, 0x01 },
			{ BalanceType.Outdoor, 0x02 },
			{ BalanceType.OnePush, 0x03 },
			{ BalanceType.AutoTrace, 0x04 },
			{ BalanceType.Manual, 0x05 }
		};

		readonly static Dictionary<CommandType, byte> cmdByteMap = new() {
			{ CommandType.None, 0x00 },
			{ CommandType.BDC_AddressSet, 0x00 },
			{ CommandType.BDC_IFClear, 0x00 },
			{ CommandType.CMD_IFClear, 0x00 },
			{ CommandType.CMD_Power, 0x00 },
			{ CommandType.CMD_PanTilt, 0x01 },
			{ CommandType.CMD_PanTiltRel, 0x03 },
			{ CommandType.CMD_PanTiltHome, 0x04 },
			{ CommandType.CMD_Zoom, 0x07 },
			{ CommandType.CMD_Memory, 0x3F },
			{ CommandType.CMD_Focus, 0x08 },
			{ CommandType.CMD_FocusMode, 0x38 },
			{ CommandType.CMD_ExposureMode, 0x39 },
			{ CommandType.CMD_ExposurePos, 0x4D },
			{ CommandType.CMD_Backlight, 0x33 },
			{ CommandType.CMD_ExpCompOn, 0x3E },
			{ CommandType.CMD_ExpCompPos, 0x4E },
			{ CommandType.CMD_BalanceMode, 0x35 },
			{ CommandType.CMD_BalanceRed, 0x43 },
			{ CommandType.CMD_BalanceBlue, 0x44 },
			{ CommandType.INQ_Power, 0x00 },
			{ CommandType.INQ_FocusMode, 0x38 },
			{ CommandType.INQ_AEMode, 0x39 },
			{ CommandType.INQ_BrightPos, 0x4D },
			{ CommandType.INQ_BacklightMode, 0x33 },
			{ CommandType.INQ_Memory, 0x3F },
			{ CommandType.INQ_BalanceMode, 0x35 },
			{ CommandType.INQ_BalanceRed, 0x43 },
			{ CommandType.INQ_BalanceBlue, 0x44 },
			{ CommandType.INQ_ExpCompOn, 0x3E },
			{ CommandType.INQ_ExpCompPos, 0x4E },
			{ CommandType.INQ_DeviceType, 0x02 },
			{ CommandType.INQ_PanTiltPos, 0x12 }
		};

		readonly Dictionary<int, string> vendorMap = new() {
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

		readonly Dictionary<int, string> sonyModelMap = new() {
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
		readonly Dictionary<int, string> averModelMap = new() {
			{ 0x559, "MD330" },
			{ 0x565, "MD120" },
			{ 0x500, "PTZ210" },
			{ 0x510, "PTC500" },
			{ 0x505A, "CAM520" }
		};

		readonly uint NoWBManual = 0x01;
		readonly	uint NoBrightDirect = 0x02;

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
		readonly static Queue<byte> receiveQueue = new();

		static int numDevices = 0;
		static uint deviceId = 1;
		static bool powerOn = false;
		readonly static List<DeviceInfo> deviceInfos = [];

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
		static bool inBalanceSetup = false;
		static bool inDisplayBright = false;

		Microsoft.UI.Windowing.AppWindow? appWindow = null;

		#endregion

		#region UI Elements

		public List<Button> presetButtons = [];
		public List<TextBox> presetTexts = [];
		public List<Panel> presetPanels = [];
		public List<Panel> cameraPanels = [];
		public List<TextBox> cameraTexts = [];
		//public List<Control> focusControls = [];
		//public List<Control> exposureControls = [];

		#endregion

		#region Constants
		const int addressBase = 0x80;
		const byte normCmd = 0x04;
		const byte panTiltCmd = 0x06;

		#endregion

		private static System.Timers.Timer? panTiltTimer;

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

		public void DFO(string txt) {
			if (loggingOn) {
#pragma warning disable CS4014
				SimpleLogger.LogAsync(txt);
#pragma warning restore CS4014
			}

			if (debugOn) {
				ResponseListAdd(txt);
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

			Init();
		}

		private void ContentLoaded(object sender, RoutedEventArgs e) {
			string result = _service.Connect();

			ShowConnectedState(result == "");
			if (result == "") {
				ShowConnectedState(true);
				DFO("Connected");
				BroadcastAddress();
			} else {
				ShowConnectedState(false);
				DFO(result);
			}

			SetControlsState();

			loaded = true;
		}

		private void Init() {
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

			DFO("start");

			presetButtons = [p1Btn, p2Btn, p3Btn, p4Btn, p5Btn, p6Btn];
			presetTexts = [p1TextBox, p2TextBox, p3TextBox, p4TextBox, p5TextBox, p6TextBox];
			presetPanels = [p1Panel, p2Panel, p3Panel, p4Panel, p5Panel, p6Panel];
			cameraPanels = [CP1, CP2, CP3, CP4, CP5, CP6, CP7];
			cameraTexts = [CT1, CT2, CT3, CT4, CT5, CT6, CT7];

			foreach (Button b in  presetButtons) {
				b.AddHandler( UIElement.PointerPressedEvent, new PointerEventHandler(PresetDown), handledEventsToo: true );
				b.AddHandler( UIElement.PointerReleasedEvent, new PointerEventHandler(PresetUp), handledEventsToo: true );
			}

			string[] presets = Config.GetPresets(deviceId - 1);
			for (int i = 0; i < presets.Length; i++) {
				presetTexts[i].Text = presets[i];
			}

			C1.IsChecked = true;

			DFO("init");
		}

		private void SetControlsState() {
			this.DispatcherQueue.TryEnqueue(() => {
				bool enable = false;
				if ((_service != null) && _service.IsConnected) {
					enable = true;
					//ShowPowerState(true);
				} else {
					enable = false;
					//ShowPowerState(false);
				}

				foreach (Button b in presetButtons) {
					b.IsEnabled = enable;
				}

				foreach (TextBox t in presetTexts) {
					t.IsEnabled = enable;
				}

				DisplayBrightMode(false);
				DisplayExpComp(false);
				BalanceSetup();
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
			bool debug = Config.Debug;
			SettingsDialog dialog = new() { XamlRoot = this.Content.XamlRoot }; // Required for WinUI 3
			await dialog.ShowAsync();

			if (debug != Config.Debug) {
				appWindow?.Resize(new Windows.Graphics.SizeInt32(510, Config.Debug ? 850 : 510));
			}
		}

		private void ConnectClick(object sender, RoutedEventArgs e) {
			DFO("ConnectClick()");
			if (_service.IsConnected) {
				DFO("disconnect");
				_service.Disconnect();
				ShowConnectedState(false);
			} else {
				try {
					DFO("connect");
					_service.Connect();
					ShowConnectedState(true);
					BroadcastAddress();
				} catch (Exception exc) {
					DFO("Exception connecting: " + exc.Message);
				}
			}

			SetControlsState();
		}

		private void PowerClick(object sender, RoutedEventArgs e) {
			if (powerOn) {
				byte[] d = [ 0x03 ];
				SendCommand(CommandType.CMD_Power, d, "off");
				ShowPowerState(false);
			} else {
				byte[] d = [ 0x02 ];
				SendCommand(CommandType.CMD_Power, d, "on");
				ShowPowerState(true);
				SetControlsState();
			}
		}

		#region Send
		private static string DataToHex(byte[] data) {
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
					DFO($"QUE: {msg.cmdType,-25} - {DataToHex(msg.data)}");
					msgQueue.Enqueue(msg);
				}
			} else if (!connectionFailed) {
				connectionFailed = true;
				DFO("port is not connected");
				_service.Disconnect();
				msgQueue.Clear();
				lastCmdType = CommandType.None;
				ShowConnectedState(false);
				SetControlsState();

				ShowMessage("Port disconnected ");
			}
		}

		private void ProcessSendMsg(VMessage msg) {
			lastCmdType = msg.cmdType;
			List<byte> msgData = [];
			byte typeByte = normCmd;

			switch (msg.msgType) {
				case MessageType.MSG_Broadcast:
					msgData.Add(0x88);
					break;
				case MessageType.MSG_Command:
					msgData.Add(GetAddressByte());
					msgData.Add(0x01);
					if ((msg.cmdType == CommandType.CMD_PanTilt) 
							|| (msg.cmdType == CommandType.CMD_PanTiltRel) 
							|| (msg.cmdType == CommandType.CMD_PanTiltHome)) {
						typeByte = panTiltCmd;
					}
					msgData.Add(typeByte);
					msgData.Add(cmdByteMap[msg.cmdType]);
					break;
				case MessageType.MSG_Inquiry:
					msgData.Add(GetAddressByte());
					msgData.Add(0x09);
					if (msg.cmdType == CommandType.INQ_DeviceType) {
						typeByte = 0x00;
					} else if (msg.cmdType == CommandType.INQ_PanTiltPos) {
						typeByte = panTiltCmd;
					}
					msgData.Add(typeByte);
					msgData.Add(cmdByteMap[msg.cmdType]);
					break;
				default:
					DFO("Unknown message type: " + msg.msgType.ToString());
					return;
			}

			msgData.AddRange(msg.data);
			msgData.Add(0xFF);
			byte[] ary = [..msgData];
			_service.Write(ary);

			string info = msg.cmdType.ToString() + (msg.comment.Length > 0 ? (", " + msg.comment) : "");
			DFO($"SND: {info,-25} - {DataToHex(ary)}");
			lastSend = DateTime.Now;
			receiveTimer = new System.Timers.Timer(3000) { Enabled = true };
			receiveTimer.Elapsed += ReceiveTimer_Tick;
		}

		private static byte GetAddressByte() {
			return (byte)(addressBase | deviceId);
		}
		private void SendCommand(CommandType cmdType, byte[] data, string more = "") {
			VMessage msg = new (MessageType.MSG_Command, cmdType, GetAddressByte(), data, more);
			SendMsg(msg);
		}

		private void SendInquiry(CommandType cmdType, string more = "") {
			VMessage msg = new (MessageType.MSG_Inquiry, cmdType, GetAddressByte(), [], more);
			SendMsg(msg);
		}

		private  void BroadcastAddress() {
			VMessage msg = new (MessageType.MSG_Broadcast, CommandType.BDC_AddressSet, 0x88, [ 0x30, 0x01 ]);
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
				List<byte> response = [];
				do {
					response.Add(receiveQueue.Dequeue());
				} while (response[^1] != 0xFF);
				string str = "";
				foreach (byte b in response) {
					str += b.ToString("X2") + " ";
				}
				string receiveString = response.Count switch {
					 3  => Handle3ByteResponse(response),
					 4  => "RCV: " + Handle4ByteResponse(response),
					 7  => "RCV: " + Handle7ByteResponse(response),
					 10 => "RCV: " + Handle10ByteResponse(response),
					 11 => "RCV: " + Handle11ByteResponse(response),
					 _  => "Unknown"
				};
				if (receiveString.Length > 0) {
					DFO($"{receiveString,-30} - {str}");
				}
			}

			if (lastCmdType == CommandType.None) {
				if (!msgQueue.Empty) {
					VMessage msg = new();
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
			SetControlsState();
			DFO("Port closed - no response from " + lastCmdType.ToString());
		}

		private async void ShowMessage(string msg) {
			if (messageDialogShowing) {
				DFO("message dialog already showing");
				return;
			}
		
			messageDialogShowing = true;
			DFO("Show: " + msg);
			try {
				this.DispatcherQueue.TryEnqueue(() => {
					ContentDialog dialog = new() {
						Title = "ViscaUI Message",
						Content = msg,
						CloseButtonText = "OK",
						XamlRoot = ContentFrame.XamlRoot // Critical requirement
					};

					dialog.Closed += ContentDialog_Closed;
					_ = dialog.ShowAsync();
				});
			} catch (Exception exc) {
				DFO($"show message exception: {exc}");
			}
		}

		private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args) {
			DFO("Dialog closed");
			messageDialogShowing = false;
		}

		private  void Service_ErrorOccurred(object? sender, string message) {
			DFO("Service error occurred");
			ShowMessage("Service error occurred");
		}

		private  async void Service_ConnectionLost(object? sender, EventArgs e) {
			DFO("Connection Lost");
			ShowMessage("Connection Lost");
		}

		private  string Handle3ByteResponse(List<byte> response) {
			string rtn = "";
			int dev = ((int)((response[0] >> 4) & 0x7));
			if (response[1] == 0x38) {
				rtn = $"CHG: Device {dev}";
				BroadcastAddress();
			} else if ((response[1] & 0xF0) == 0x40) {
				//rtn = $"ACK: Device {dev}";
			} else if ((response[1] & 0xF0) == 0x50) {
				rtn = $"FIN: Device {dev}";
				lastCmdType = CommandType.None;
			}

			return rtn;
		}

		private  string Handle4ByteResponse(List<byte> response) {
			string rtn = "";
			try {
				if ((response[0] == 0x88) && (response[1] == 0x30)) {
					rtn = "Address Set Return";
					numDevices = (int)(response[2]) - 1;
					if (numDevices == 0) numDevices = 1;		// handle adverse case where return is same as sent message
					lastCmdType = CommandType.None;
					ShowValidCameras();
					SendInquiry(CommandType.INQ_DeviceType);
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
									SendInquiry(CommandType.INQ_AEMode);
									SendInquiry(CommandType.INQ_FocusMode);
									SendInquiry(CommandType.INQ_BalanceMode);
								} else if (response[2] == 0x03) {
									ShowPowerState(false);
									rtn = "Power Off";
								}
								lastCmdType = CommandType.None;
								SetControlsState();
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
									DisplayBrightMode(false);
									rtn = "Exposure Auto ";
								} else if (response[2] == 0x0D) {
									DisplayBrightMode(true);
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
										//if ((deviceInfos is not null) && ((deviceInfos[(int)deviceId - 1].restrict & NoWBManual) == 0)) {
											bal = BalanceType.Manual;
										//}
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
								DisplayExpComp(on);
								lastCmdType = CommandType.None;
								SendInquiry(CommandType.INQ_ExpCompPos);
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

		private  string Handle7ByteResponse(List<byte> response) {
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

		private  string Handle10ByteResponse(List<byte> response) {
			string rtn = "";
			switch (lastCmdType) {
				case CommandType.INQ_DeviceType:
					byte dev = (byte)((response[0] >> 4) & 0x7);
					int vendorId = ((int)response[2] << 8) + response[3];
					int modelId = ((int)response[4] << 8) + response[5];
					int version = ((int)response[6] << 8) + response[7];
					string vendorStr = vendorMap.TryGetValue(vendorId, out string? value) ? value : "Unknown";
					string modelStr = "Unknown";
					if (vendorStr == "Sony") {
						modelStr = sonyModelMap.TryGetValue(modelId, out string? value1) ? value1 : "Unknown";
					} else if (vendorStr == "AVer") {
						modelStr = averModelMap.TryGetValue(modelId, out string? value1) ? value1 : "Unknown";
					}
					string versionStr = version.ToString("X2");
					uint restrict = 0;
					if (modelStr  == "EVI-D30") {
						restrict |= NoWBManual;
						restrict |= NoBrightDirect;
					}
					DeviceInfo devInfo = new(dev, "", vendorStr, modelStr, versionStr, restrict);
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
						SendInquiry(CommandType.INQ_DeviceType);
					} else {
						deviceId = 1;
						SendInquiry(CommandType.INQ_Power);
					}
					break;
			}

			return rtn;
		}

		private static string Handle11ByteResponse(List<byte> response) {
			string rtn = "";
			if (response[1] == 0x50) {  // inquiry response
				if (lastCmdType == CommandType.INQ_PanTiltPos) {
					int pan = ((response[2] << 12) | (response[3] << 8) | (response[4] << 4) | response[5]);
					int tilt = ((response[6] << 12) | (response[7] << 8) | (response[8] << 4) | response[9]);
					rtn = $"Pan {pan}, Tilt {tilt}";
					lastCmdType = CommandType.None;
				}
			}
			return rtn;
		}

		private  void ResponseListAdd(string text) {
			this.DispatcherQueue.TryEnqueue(() => {
				responseListBox.Items.Insert(0, text);
			});
		}
		#endregion

		#region Pan Tilt
		private  void PanTiltStop() {
			byte[] d = [ panRate, tiltRate, 0x03, 0x03 ];
			SendCommand(CommandType.CMD_PanTilt, d, "stop");
		}

		private void PanTiltStart(byte b6, byte b7) {
			byte[] d = [ panRate, tiltRate, b6, b7 ];
			SendCommand(CommandType.CMD_PanTilt, d, $"p {panRate}, t {tiltRate}");
		}

		private void PanTiltMove(int pan, int tilt) {
			byte[] d = [ panRate, tiltRate, 0, 0, 0, 0, 0, 0, 0, 0 ];
			int panRem = pan * panRate;
			for (int i = 3; i >= 0; i--) {
				d[i + 2] = (byte)(panRem & 0x0F);
				panRem >>= 4;
			}
			int tiltRem = tilt * tiltRate;
			for (int i = 3; i >= 0; i--) {
				d[i + 6] = (byte)(tiltRem & 0x0F);
				tiltRem >>= 4;
			}
			SendCommand(CommandType.CMD_PanTiltRel, d, $"p {panRate}, t {tiltRate}");
		}

		private void CenterBtnClick(object sender, RoutedEventArgs e) {
			byte[] d = [];
			SendCommand(CommandType.CMD_PanTiltHome, d, "center");
		}

		PointerPoint? ptStartPoint = null;

		private void PtRect_MouseDown(object _1, PointerRoutedEventArgs e) {
			Debug.WriteLine("mouse down");
			panTiltTimer = new System.Timers.Timer(300) { Enabled = true };
			panTiltTimer.Elapsed += PanTiltTimer_Tick;
			ptStartPoint = e.GetCurrentPoint(ptRect);
			Debug.WriteLine("mouse down end");
		}

		private void PtRect_MouseUp(object _1, PointerRoutedEventArgs e) {
			Debug.WriteLine("mouse up");
			ptRectDragging = false;
			if (panTiltTimer is not null) {
				panTiltTimer.Stop();
				panTiltTimer.Dispose();
				PointerPoint point = e.GetCurrentPoint(ptRect);
				Point pos = point.Position;
				double x = Math.Max(Math.Min(pos.X, ptRectWidth), 0) - (ptRectWidth / 2);
				double y = Math.Max(Math.Min(pos.Y, ptRectHeight), 0) - (ptRectHeight / 2);
				panRate = GetPanRate(x);
				tiltRate = GetTiltRate(y);

				int lr = 0;
				int ud = 0;
				if (panRate != 0) {
					lr = ((x < 0) ? -1 : 1);
				}
				if (tiltRate != 0) {
					ud = ((y > 0) ? -1 : 1);
				}
				if ((lr != 0) || (ud != 0)) {
					PanTiltMove(lr, ud);
					if (lastPresetNumber != -1) {
						this.DispatcherQueue.TryEnqueue(() => {
							presetPanels[lastPresetNumber].Background = new SolidColorBrush(Colors.Transparent);
							lastPresetNumber = -1;
						});
					}
				}

				// move camera small amount
			} else {
				PanTiltStop();
				lastPanRate = 0;
				lastTiltRate = 0;
			}
			Debug.WriteLine("mouse up end");
		}

		private void PanTiltTimer_Tick(object? sender, object e) {
			Debug.WriteLine("pt timer");
			if (panTiltTimer is not null) {
				Debug.WriteLine("pt timer not null");
				panTiltTimer.Stop();
				panTiltTimer.Dispose();
				panTiltTimer = null;
				ptRectDragging = true;
				this.DispatcherQueue.TryEnqueue(() => {
					PtHandleChange(ptStartPoint);
				});
			}
			Debug.WriteLine("pt timer end");
		}


		const int ptRectWidth = 140;
		const int ptInc = 10;
		const int ptRectHeight = 120;
		private static readonly int[] panRates = [ 0, 1, 3, 6, 10, 15, 24 ];
		private static readonly int[] tiltRates = [ 0, 1, 3, 6, 10, 1 ];

		private static byte GetPanRate(double x) {
			double ax = Math.Abs(x);
			int xInd = Math.Min((int)(ax / ptInc), 6);
			return (byte)panRates[xInd];
		}

		private static byte GetTiltRate(double y) {
			double ay = Math.Abs(y);
			int yInd = Math.Min((int)(ay / ptInc), 5);
			return (byte)tiltRates[yInd];
		}

		private void PtHandleChange(PointerPoint? point) {
			if (point is null) { return; }
			Point pos = point.Position;
			double x = Math.Max(Math.Min(pos.X, ptRectWidth), 0) - (ptRectWidth / 2);
			double y = Math.Max(Math.Min(pos.Y, ptRectHeight), 0) - (ptRectHeight / 2);
			panRate = GetPanRate(x);
			tiltRate = GetTiltRate(y);

			bool change = false;
			if (lastPanRate != panRate) {
				lastPanRate = panRate;
				change = true;
			}
			if (lastTiltRate != tiltRate) {
				lastTiltRate = tiltRate;
				change = true;
			}

			if (change) {
				if ((panRate == 0) && (tiltRate == 0)) {
					PanTiltStop();
				} else {
					byte lr = (byte)((panRate == 0) ? 3 : ((x < 0) ? 1 : 2));
					byte ud = (byte)((tiltRate == 0) ? 3 : ((y < 0) ? 1 : 2));
					PanTiltStart(lr, ud);
				}
				if (lastPresetNumber != -1) {
					this.DispatcherQueue.TryEnqueue(() => {
						presetPanels[lastPresetNumber].Background = new SolidColorBrush(Colors.Transparent);
						lastPresetNumber = -1;
					});
				}
			}
		}

		private void PtRect_MouseMove(object _1, PointerRoutedEventArgs e) {
			if (ptRectDragging) {
				PointerPoint ptrPt = e.GetCurrentPoint(ptRect);
				PtHandleChange(ptrPt);
			}
		}
		#endregion

		#region Zoom
		private void ZoomStop() {
			byte[] d = [ 0x00 ];
			SendCommand(CommandType.CMD_Zoom, d, "stop");
		}

		private void ZoomIn() {
			byte cmd = (byte)(0x20 | zoomRate);
			byte[] d = [ cmd ];
			SendCommand(CommandType.CMD_Zoom, d, $"in {zoomRate}");
		}

		private void ZoomOut() {
			byte cmd = (byte)(0x30 | zoomRate);
			byte[] d = [ cmd ];
			SendCommand(CommandType.CMD_Zoom, d, $"out {zoomRate}");
		}

		private void ZoomRect_MouseDown(object sender, PointerRoutedEventArgs e) {
			zmRectDragging = true;
			ZoomRect_MouseMove(sender, e);
		}

		private void ZoomRect_MouseUp(object sender, PointerRoutedEventArgs e) {
			zmRectDragging = false;
			ZoomStop();
			lastZoomRate = 0;
		}

		const int zoomInc = 12;
		const int zoomRectHeight = 120;
		private static readonly int[] zoomRates = [ 0, 1, 2, 4, 7 ];

		private void ZoomRect_MouseMove(object sender, PointerRoutedEventArgs e) {
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
						ZoomStop();
					} else {
						if (y < 0) {
							ZoomIn();
						} else {
							ZoomOut();
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

		private void SetFocusType(bool manual) {
			focusRect.Visibility = manual ? Visibility.Visible : Visibility.Collapsed;
			if (lastCmdType == CommandType.None) {
				byte[] d = [ (byte)(manual ? 0x03 : 0x02) ];
				SendCommand(CommandType.CMD_FocusMode, d, $"{(manual ? "manual" : "auto")}");
			}
		}

		private void FocusStop() {
			byte[] d = [ 0x00 ];
			SendCommand(CommandType.CMD_Focus, d, "stop");
		}

		private void FocusIn() {
			byte cmd = (byte)(0x30 | focusRate);
			byte[] d = [ cmd ];
			SendCommand(CommandType.CMD_Focus, d, $"in {focusRate}");
		}

		private void FocusOut() {
			byte cmd = (byte)(0x20 | focusRate);
			byte[] d = [ cmd ];
			SendCommand(CommandType.CMD_Focus, d	, $"out {focusRate}");
		}
		private void FocusManualClick(object sender, RoutedEventArgs e) {
			SetFocusType(focusManual.IsChecked == true);
		}

		private void FocusRect_MouseDown(object sender, PointerRoutedEventArgs e) {
			focusRectDragging = true;
			FocusRect_MouseMove(sender, e);
		}
		private void FocusRect_MouseUp(object sender, PointerRoutedEventArgs e) {
			focusRectDragging = false;
			FocusStop();
			lastFocusRate = 0;
		}

		const int focusInc = 9;
		const int focusRectHeight = 90;
		private static readonly int[] focusRates = [ 0, 1, 2, 4, 7 ];

		private void FocusRect_MouseMove(object sender, PointerRoutedEventArgs e) {
			if (focusRectDragging) {
				PointerPoint ptrPt = e.GetCurrentPoint(focusRect);
				Point pos = ptrPt.Position;
				double y = Math.Max(Math.Min(pos.Y, focusRectHeight), 0) - (focusRectHeight / 2);
				double ay = Math.Abs(y);
				int yInd = Math.Min((int)(ay / focusInc), 4);
				int fr = focusRates[yInd];

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
						FocusStop();
					} else {
						if (y < 0) {
							FocusIn();
						} else {
							FocusOut();
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

					SendInquiry(CommandType.INQ_Power);
				}
			}
		}

		private void ShowValidCameras() {
			this.DispatcherQueue.TryEnqueue(() => {
				for (int i = 0; i < 7; i++) {
					if (i < numDevices) {
						cameraPanels[i].Visibility = Visibility.Visible;
						string text = Config.GetCamera((uint)i);
						cameraTexts[i].Text = text;
					} else {
						cameraPanels[i].Visibility = Visibility.Collapsed;
					}
				}
			});
		}

		private void CameraTextChanged(object sender, RoutedEventArgs _1) {
			TextBox? textBox = sender as TextBox;
			if (textBox is not null) {
				string name = textBox.Name.ToString();
				name = name.Replace("CT", "");
				int cameraNumber = (name is not null) ? int.Parse(name) : 0;
				Debug.WriteLine($"Camera number: {cameraNumber}");
				Config.SetCamera((uint)cameraNumber - 1, textBox.Text);
			}
		}
		#endregion

		#region Presets
		private void PresetDown(object sender, PointerRoutedEventArgs e) {
			lastPreset = (Button)sender;
			string? lpStr = lastPreset.Content.ToString();
			lastPresetNumber = (lpStr is not null) ? int.Parse(lpStr) - 1 : 0;
			settingPreset = false;
			presetTimer = new System.Timers.Timer(1000) { Enabled = true };
			presetTimer.Elapsed += PresetTimer_Tick;
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
				HandlePreset((byte)(btnNbr), presetTimer);
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

		private void HandlePreset(byte number, System.Timers.Timer? presetTimer1) {
			this.DispatcherQueue.TryEnqueue(() => {
				for (int i = 0; i < 6; i++) {
					presetPanels[i].Background = new SolidColorBrush(Colors.Transparent);
				}
				presetButtons[number].Background = new SolidColorBrush(Colors.Azure);
				presetPanels[number].Background = new SolidColorBrush(Colors.Maroon);
			});

			byte[] d = [ 0x01, number ];
			d[0] = settingPreset ? (byte)0x01 : (byte)0x02;
			SendCommand(CommandType.CMD_Memory, d, $"preset {number + 1}");

			if (presetTimer1 is not null) {
				presetTimer1.Enabled = false;
			}
			lastPreset = null;
			lastPresetNumber = number;

			if (!settingPreset) {
				SendInquiry(CommandType.INQ_AEMode);
				SendInquiry(CommandType.INQ_FocusMode);
				SendInquiry(CommandType.INQ_BalanceMode);
				SendInquiry(CommandType.INQ_ExpCompOn);
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
		private void ExpManualClick(object _1, RoutedEventArgs _2) {
			SetBrightType(expManualChk.IsChecked == true);
		}

		private void DisplayBrightMode(bool manual) {
			inDisplayBright = true;
			this.DispatcherQueue.TryEnqueue(() => {
				expManualChk.IsChecked = manual;
				expSlider.IsEnabled = manual;
				//brightBtn.Enabled = manual;
				//darkBtn.Enabled = manual;
				expBacklitChk.IsEnabled = !manual;
			});
			inDisplayBright = false;
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

		private void SetBrightType(bool manual) {
			if (inDisplayBright) return;

			DisplayBrightMode(manual);

			if (manual) {
				SetExpComp(false);
			} else {
				ShowExposure(0);
			}

			byte[] d = [ (byte)(manual ? 0x0D : 0x00) ];
			SendCommand(CommandType.CMD_ExposureMode, d, $"{(manual ? "Manual" : "Auto")}");

			if (!manual) {
				SendInquiry(CommandType.INQ_BacklightMode);
			} else {
				SendInquiry(CommandType.INQ_BrightPos);
			}
		}

		private void ExpSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			if (lastCmdType == CommandType.None) {
				int value = (int)e.NewValue;
				byte p = (byte)((value >> 4) & 1);
				byte q = (byte)(value & 0x0F);
				byte[] d = [ 0x00, 0x00, p, q ];
				SendCommand(CommandType.CMD_ExposurePos, d, $"{value}");
				ShowExposure(value);
			}
		}

		private void ExpBacklitClick(object sender, RoutedEventArgs e) {
			bool? chk = expBacklitChk.IsChecked;
			if (chk.HasValue) {
				SetBacklight(chk.Value);
			}
		}

		private void SetBacklight(bool on) {
			if (lastCmdType == CommandType.None) {
				byte[] d = [ (byte)(on ? 0x02 : 0x03), 0xFF ];
				SendCommand(CommandType.CMD_Backlight, d, $"{(on ? "on" : "off")}");
			}
		}

		private void ExpCompClick(object sender, RoutedEventArgs e) {
			bool? chk = expCompChk.IsChecked;
			if (chk.HasValue) {
				SetExpComp(chk.Value);
			}
		}

		private void SetExpComp(bool on) {
			DisplayExpComp(on);

			byte[] d = [ (byte)(on ? 0x02 : 0x03) ];
			SendCommand(CommandType.CMD_ExpCompOn, d, $"{(on ? "on" : "off")}");

			if (on) {
				SendInquiry(CommandType.INQ_ExpCompPos);
			}
		}

		private void DisplayExpComp(bool on) {
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
			byte[] d = [ 0x00, 0x00, p, q ];
			SendCommand(CommandType.CMD_ExpCompPos, d, $"{value}");
			ShowExposureComp(value);
		}
		#endregion

		#region White Balance
		private void BalanceSetup() {
			inBalanceSetup = true;
			wbSelectCombo.Items.Clear();
			wbSelectCombo.Items.Add(balanceStrMap[BalanceType.Auto]);
			wbSelectCombo.Items.Add(balanceStrMap[BalanceType.Indoor]);
			wbSelectCombo.Items.Add(balanceStrMap[BalanceType.Outdoor]);
			if (deviceId <= deviceInfos.Count) {
				uint restrict = deviceInfos[(int)deviceId - 1].restrict;
				bool manualOk = ((restrict & NoWBManual) == 0);
				if (manualOk) {
					wbSelectCombo.Items.Add(balanceStrMap[BalanceType.Manual]);
				}

				wbSelectCombo.SelectedIndex = 0;
				wbRedSlider.IsEnabled = false;
				wbBlueSlider.IsEnabled = false;
			}
			inBalanceSetup = false;
		}

		private void SetBalanceType(BalanceType balance) {
			balanceType = balance;

			if (wbSelectCombo.Items.Count > 0) {
				bool manual = ((BalanceType)balance == BalanceType.Manual);
				wbRedSlider.IsEnabled = manual;
				wbBlueSlider.IsEnabled = manual;

				if (manual) {
					SendInquiry(CommandType.INQ_BalanceRed);
					SendInquiry(CommandType.INQ_BalanceBlue);
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
				byte[] d = [ 0x00, 0x00, p, q ];
				SendCommand(CommandType.CMD_BalanceRed, d, $"{value}");
				ShowGainText(wbRedText, value);
			}
		}

		private void BalBlueSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			if (loaded) {
				int value = (int)e.NewValue;
				byte p = (byte)(value >> 4);
				byte q = (byte)(value & 0x0f);
				byte[] d = [ 0x00, 0x00, p, q ];
				SendCommand(CommandType.CMD_BalanceBlue, d, $"{value}");
				ShowGainText(wbBlueText, value);
			}
		}

		private void BalSelectChanged(object sender, SelectionChangedEventArgs e) {
			if (loaded && powerOn && !inBalanceSetup) {
				BalanceType typ = BalanceType.Auto;
				string? bal = wbSelectCombo.SelectedItem as string;
				foreach (KeyValuePair<BalanceType, string> kvp in balanceStrMap) {
					if (kvp.Value == bal) {
						typ = kvp.Key;
					}
				}

				SetBalanceType(typ);
				byte[] d = [ balanceCmdMap[typ] ];
				SendCommand(CommandType.CMD_BalanceMode, d, $"{balanceStrMap[typ]}");
			}
		}

		#endregion
	}
}
