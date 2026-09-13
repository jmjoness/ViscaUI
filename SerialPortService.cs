using System;
using System.IO;
using System.IO.Ports;

namespace ViscaUI {
	public sealed partial class SerialPortService : IDisposable
	{
		private SerialPort? _port;
		private readonly object _lock = new();

		public bool IsConnected { get { lock (_lock) { return _port?.IsOpen == true; } } }

		public event EventHandler<byte[]>? DataReceived;
		public event EventHandler<string>? ErrorOccurred;
		public event EventHandler?         ConnectionLost;

		public string Connect() {
			string rtn = "";

			lock (_lock) {
				try {
					if (_port?.IsOpen == true)
						throw new InvalidOperationException("Already connected. Call Disconnect first.");

					string port = Config.Port;

					if (port == "") {
						rtn = "Port selection required";
					} else {
						_port = new SerialPort {
							PortName = Config.Port,
							BaudRate = Config.Speed,
							DataBits = 8,
							StopBits = StopBits.One,
							Parity = Parity.None,
							DtrEnable = true,
							RtsEnable = true,
							ReadTimeout = 500,
							WriteTimeout = 500
						};

						_port.DataReceived += Port_DataReceived;
						_port.ErrorReceived += Port_ErrorReceived;
						_port.Open();
						_port.DiscardInBuffer();
					}
				} catch (Exception e) { 
					rtn = e.Message;
				}
			}

			return rtn;
		}
		public SerialPortService() { }

		public void Disconnect() {
			lock (_lock) {
				if (_port is null) return;
				_port.DataReceived  -= Port_DataReceived;
				_port.ErrorReceived -= Port_ErrorReceived;
				try { if (_port.IsOpen) _port.Close(); } catch { }
			}
		}

		public void Write(byte[] data) {
			lock (_lock) {
				if (_port?.IsOpen != true) {
					throw new InvalidOperationException("Port is not open.");
				} else {
					_port.Write(data, 0, data.Length);
				}
			}
		}

		private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e) {
			try {
				SerialPort? port;
				lock (_lock) { port = _port; }
				if (port?.IsOpen != true) return;

				int count = port.BytesToRead;
				if (count <= 0) return;

				byte[] buf  = new byte[count];
				int    read = port.Read(buf, 0, count);
				if (read <= 0) return;

				if (read < count) Array.Resize(ref buf, read);
				DataReceived?.Invoke(this, buf);
			} catch (IOException ex) {
				// Device physically disconnected or cable pulled
				ErrorOccurred?.Invoke(this, ex.Message);
				ConnectionLost?.Invoke(this, EventArgs.Empty);
			} catch (InvalidOperationException ex) {
				// Port closed unexpectedly (e.g., driver removed)
				ErrorOccurred?.Invoke(this, ex.Message);
				ConnectionLost?.Invoke(this, EventArgs.Empty);
			} catch (Exception ex) {
				// Other errors (timeout, encoding) — report but don't signal ConnectionLost
				ErrorOccurred?.Invoke(this, ex.Message);
			}
		}

		private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
			ErrorOccurred?.Invoke(this, $"Serial error: {e.EventType}");
		}

		public void Dispose() {
			Disconnect();
			lock (_lock) { _port?.Dispose(); _port = null; }
		}
	}
}
