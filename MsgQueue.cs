using System;
using System.Collections.Generic;
using System.Linq;

namespace ViscaUI
{
	enum MessageType {
		MSG_Broadcast, MSG_Command, MSG_Inquiry
	}

	enum CommandType {
		None,
		BDC_AddressSet, BDC_IFClear, 
		CMD_IFClear, CMD_Power, CMD_PanTilt, CMD_PanTiltRel, CMD_PanTiltHome, 
		CMD_Zoom, CMD_Memory, 
		CMD_Focus, CMD_FocusMode, CMD_ExposureMode, CMD_ExposurePos, CMD_Backlight, 
		CMD_ExpCompOn, CMD_ExpCompPos, CMD_BalanceMode, CMD_BalanceRed, CMD_BalanceBlue,
		INQ_Power, INQ_FocusMode, INQ_AEMode, INQ_BrightPos, INQ_BacklightMode,
		INQ_Memory, INQ_BalanceMode, INQ_BalanceRed, INQ_BalanceBlue, INQ_ExpCompOn,
		INQ_ExpCompPos, INQ_DeviceType
	}

	struct VMessage {
		public MessageType msgType = MessageType.MSG_Command;
		public CommandType cmdType = CommandType.None;
		public byte address = 1;
		public byte[] data = new byte[0];
		public string comment = "";
		public int msgNum = 0;
		public VMessage() { }
		public VMessage(MessageType msgType, CommandType cmdType, byte address, byte[] data, string comment = "") {
			this.msgType = msgType;
			this.cmdType = cmdType;
			this.address = address;
			this.data = data;
			this.comment = comment;
		}
	}

	class MsgQueue
	{
		List<VMessage> list = new();

		private string dataToString(VMessage msg) {
			string msgStr = "";
			foreach (byte b in msg.data) {
				msgStr += b.ToString("X2");
			}
			return msgStr;
		}

		public void Enqueue(VMessage msg) {
			lock (this) {
				list.Add(msg);
			}
		}

		public bool Dequeue(ref VMessage msg) {
			bool rtn = false;
			lock (this) {
				if (list.Count > 0) {
					msg = list[0];
					list.RemoveAt(0);
				}
			}
			return rtn;
		}

		public void Clear() {
			lock(this) {
				list.Clear();
			}
		}

		public bool Empty {
			get {
				bool rtn = true;
				lock (this) {
					rtn = (list.Count == 0);
				}
				return rtn;
			}
		}

		public int Count {
			get { return list.Count(); }
		}
	}
}
