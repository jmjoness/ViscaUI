using System.ComponentModel.Design;
using System.Drawing;
using System.Text.Json;
using Windows.Storage;

namespace ViscaUI {
	enum Mode { D70, D30 };

	class Config {
		const string PortStr = "Port";
		const string SpeedStr = "Speed";
		const string LocXStr = "LocX";
		const string LocYStr = "LocY";
		//const string ModeStr = "Mode";
		const string DebugStr = "Debug";
		const string MiniStr = "Mini";
		//const string InitStr = "Init";
		const string LogStr = "Log";
		//const string InstanceStr = "Instance";
		readonly static string[] CameraStrs = [ "Camera0", "Camera1", "Camera2", "Camera3", "Camera4", "Camera5", "Camera6" ];
		readonly static string[] PresetStrs = ["Preset0", "Preset1", "Preset2", "Preset3", "Preset4", "Preset5"];
		readonly static string[] PresetAry = ["PresetCam1", "PresetCam2", "PresetCam3", "PresetCam4", "PresetCam5", "PresetCam6", "PresetCam7"];
		const string CalibratedStr = "Calibrated";
		const string CalibrationXMinStr = "CalibrationXMin";
		const string CalibrationXMaxStr = "CalibrationXMax";
		const string CalibrationYMinStr = "CalibrationYMin";
		const string CalibrationYMaxStr = "CalibrationYMax";
		const string CalibrationZMinStr = "CalibrationZMin";
		const string CalibrationZMaxStr = "CalibrationZMax";

		static int m_number = 0;

		static bool GetBool(string keyStr, bool defVal) {
			var localSettings = ApplicationData.Current.LocalSettings;
			bool rtn = localSettings.Values[keyStr] as bool? ?? defVal;
			return rtn;
		}

		static int GetInt(string keyStr, int defVal) {
			var localSettings = ApplicationData.Current.LocalSettings;
			int rtn = localSettings.Values[keyStr] as int? ?? defVal;
			return rtn;
		}

		static string GetString(string keyStr, string defVal) {
			var localSettings = ApplicationData.Current.LocalSettings;
			string rtn = localSettings.Values[keyStr] as string ?? defVal;
			return rtn;
		}

		static string[] GetArray(string keyStr) {
			string[] result = new string[] { "", "", "", "", "", "" };
			var localSettings = ApplicationData.Current.LocalSettings;
			if (localSettings.Values.TryGetValue(keyStr, out object storedValue) && storedValue is string jsonString) {
				result = JsonSerializer.Deserialize<string[]>(jsonString);
			}
			return result;
		}

		static bool SetBool(string keyStr, bool value) {
			var localSettings = ApplicationData.Current.LocalSettings;
			localSettings.Values[keyStr] = value;
			return value;
		}

		static int SetInt(string keyStr, int value) {
			var localSettings = ApplicationData.Current.LocalSettings;
			localSettings.Values[keyStr] = value;
			return value;
		}

		static string SetString(string keyStr, string value) {
			var localSettings = ApplicationData.Current.LocalSettings;
			localSettings.Values[keyStr] = value;
			return value;
		}

		static void SetArray(string keyStr, string[] value) {
			var localSettings = ApplicationData.Current.LocalSettings;
			string jsonString = JsonSerializer.Serialize(value);
			localSettings.Values[keyStr] = jsonString;
		}
		//static public bool Init
		//{
		//	get { return GetBool(InitStr, false); } 
		//	set { SetBool(InitStr, value); }
		//}

		static public string Port
		{
			get { return GetString(PortStr, ""); }
			set {	SetString(PortStr, value); }
		}

		static public int Speed
		{
			get { return GetInt(SpeedStr, 9600); }
			set { SetInt(SpeedStr, value); }
		}

		static public Point Location
		{
			get {
				return new Point(GetInt(LocXStr, 0), GetInt(LocYStr, 0));
			}
			set {
				SetInt(LocXStr, value.X);
				SetInt(LocYStr, value.Y);
			}
		}

		static public bool Debug
		{
			get {	return GetBool(DebugStr, false); }
			set { SetBool(DebugStr, value); }
		}

		static public bool Log
		{
			get { return GetBool(LogStr, false); }
			set { SetBool(LogStr, value); }
		}
		static public bool Mini
		{
			get { return GetBool(MiniStr, false); }
			set { SetBool(MiniStr, value); }
		}

		//static public Mode Mode
		//{
		//	get { return (GetString(ModeStr, "D70") == "D30") ? Mode.D30 : Mode.D70; }
		//	set { SetString(ModeStr, (value == Mode.D30) ? "D30" : "D70"); }
		//}

		static public bool Calibrated {
			get { return GetBool(CalibratedStr, false); }
			set { SetBool(CalibratedStr, value); }
		}

		static public int CalXMin {
			get { return GetInt(CalibrationXMinStr, 0); }
			set { SetInt(CalibrationXMinStr, value); }
		}

		static public int CalXMax {
			get { return GetInt(CalibrationXMaxStr, 65535); }
			set { SetInt(CalibrationXMaxStr, value); }
		}

		static public int CalYMin {
			get { return GetInt(CalibrationYMinStr, 0); }
			set { SetInt(CalibrationYMinStr, value); }
		}

		static public int CalYMax {
			get { return GetInt(CalibrationYMaxStr, 65535); }
			set { SetInt(CalibrationYMaxStr, value); }
		}

		static public int CalZMin {
			get { return GetInt(CalibrationZMinStr, 0); }
			set { SetInt(CalibrationZMinStr, value); }
		}

		static public int CalZMax {
			get { return GetInt(CalibrationZMaxStr, 65535); }
			set { SetInt(CalibrationZMaxStr, value); }
		}

		static public string GetCamera(uint n)
		{
			if (n < 7) {
				return GetString(CameraStrs[n], "");
			} else {
				return "";
			} 
		}

		static public void SetCamera(uint n, string str)
		{
			if (n < 7) {
				SetString(CameraStrs[n], str);
			}
		}

		static public string GetPreset(uint n) {
			if (n < 6) {
				return GetString(PresetStrs[n], "");
			} else {
				return "";
			}
		}

		static public void SetPreset(uint n, string str) {
			if (n < 6) {
				SetString(PresetStrs[n], str);
			}
		}

		static public string[] GetPresets(uint n) {
			if (n < 7) {
				return GetArray(PresetAry[n]);
			} else {
				return new string[] { "", "", "", "", "", "" };
			}
		}

		static public void SetPresets(uint n, string[] str) {
			if (n < 6) {
				SetArray(PresetAry[n], str);
			}
		}

		//static public int Instance
		//{
		//	get { return GetInt(InstanceStr, 0); }
		//	set { SetInt(InstanceStr, value); }
		//}

		public Config()
		{
			m_number = 0;
		}

		public Config(int number)
		{
			m_number = number;
		}
	}
}
