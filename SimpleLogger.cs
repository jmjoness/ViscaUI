using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ViscaUI {
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
				DateTime oldestDate = DateTime.Now;

				foreach (var file in logFiles) {
					string fileName = Path.GetFileName(file);
					int n = 0;
					string nStr = fileName.Replace("Visca-", "").Replace(".log", "");
					if (Int32.TryParse(nStr, out n)) {
						DateTime created = File.GetCreationTime(file);
						DateTime modified = File.GetLastWriteTime(file);
						DateTime accessed = File.GetLastAccessTime(file);
						if (created < oldestDate) {
							oldestDate = created;
							logNo = n;
						}
					}
				}

				if (logNo > 10) {
					logNo = 1;
				}

				initialized = true;

				int nextLog = logNo;
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
}