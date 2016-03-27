/*
 * Auto-generated code, do not modify.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;

namespace SpaceLeap
{
	public class MavLink
	{
		#region Configuration

		public static byte SystemId = 0;
		public static byte ComponentId = 0;

		#endregion

		#region Enums

		/*ENUMS*/

		#endregion

		#region Base classes

		public class MessageBase : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			public enum MessageType
			{
/*MESSAGETYPEENUM*/			}

			protected static Dictionary<byte, Type> _messageTypes = new Dictionary<byte, Type>();

			private static byte _packageSequence = 0;

			protected byte _messageId;
			protected object[] _fields;
			protected Type[] _fieldTypes;
			protected int[] _arrayLengths;
			protected int _payloadLength;

			public int MessageId
			{
				get
				{
					return _messageId;
				}
			}

			public MessageType Message
			{
				get
				{
					return (MessageType)_messageId;
				}
			}

			private void WriteValue(BinaryWriter writer, Type type, object value)
			{
				if (type.Equals(typeof(float)))
					writer.Write((float)value);
				else if (type.Equals(typeof(double)))
					writer.Write((double)value);
				else if (type.Equals(typeof(byte)))
					writer.Write((byte)value);
				else if (type.Equals(typeof(sbyte)))
					writer.Write((sbyte)value);
				else if (type.Equals(typeof(Int16)))
					writer.Write((Int16)value);
				else if (type.Equals(typeof(UInt16)))
					writer.Write((UInt16)value);
				else if (type.Equals(typeof(Int32)))
					writer.Write((Int32)value);
				else if (type.Equals(typeof(UInt32)))
					writer.Write((UInt32)value);
				else if (type.Equals(typeof(Int64)))
					writer.Write((Int64)value);
				else if (type.Equals(typeof(UInt64)))
					writer.Write((UInt64)value);
			}

			private void WriteField(BinaryWriter writer, int index)
			{
				Type type = _fieldTypes[index];
				if (type.IsArray)
				{
					int arrayLength = _arrayLengths[index];
					Array array = (Array)_fields[index];
					if (array.Length > 0)
					{
						Type nestedType = null;
						for (int i = 0; i < array.Length; ++i)
						{
							object item = array.GetValue(i);
							if (item != null)
							{
								nestedType = item.GetType();
								break;
							}
						}
						if (nestedType != null)
						{
							for (int i = 0; i < Math.Min(array.Length, arrayLength); ++i)
							{
								WriteValue(writer, nestedType, array.GetValue(i));
							}

							// Fill the array with zeros for might missing values
							object defaultValue = Convert.ChangeType(0, nestedType);
							for (int i = array.Length; i < arrayLength; ++i)
							{
								WriteValue(writer, nestedType, defaultValue);
							}
						}
					}
				}
				else
					WriteValue(writer, type, _fields[index]);
			}

			private object ReadValue(BinaryReader reader, Type type)
			{
				object value = null;
				if (type.Equals(typeof(float)))
					value = reader.ReadSingle();
				else if (type.Equals(typeof(double)))
					value = reader.ReadDouble();
				else if (type.Equals(typeof(byte)))
					value = reader.ReadByte();
				else if (type.Equals(typeof(sbyte)))
					value = reader.ReadSByte();
				else if (type.Equals(typeof(Int16)))
					value = reader.ReadInt16();
				else if (type.Equals(typeof(UInt16)))
					value = reader.ReadUInt16();
				else if (type.Equals(typeof(Int32)))
					value = reader.ReadInt32();
				else if (type.Equals(typeof(UInt32)))
					value = reader.ReadUInt32();
				else if (type.Equals(typeof(Int64)))
					value = reader.ReadInt64();
				else if (type.Equals(typeof(UInt64)))
					value = reader.ReadUInt64();

				return value;
			}

			private void ReadField(BinaryReader reader, int index)
			{
				Type type = _fieldTypes[index];
				if (type.IsArray)
				{
					int arrayLength = _arrayLengths[index];

					// Create an array instance
					Array array = (Array)Activator.CreateInstance(type, arrayLength);
					Type nestedType = array.GetValue(0).GetType();
					for (int i = 0; i < arrayLength; ++i)
						array.SetValue(ReadValue(reader, nestedType), i);

					_fields[index] = array;
				}
				else
				{
					_fields[index] = ReadValue(reader, type);
				}
			}

			public virtual byte[] Serialize()
			{
				FrameworkBitConverter converter = new FrameworkBitConverter();
				converter.SetDataIsLittleEndian(BitConverter.IsLittleEndian);

				byte[] data = null;

				using (MemoryStream stream = new MemoryStream())
				{
					using (BinaryWriter writer = new BinaryWriter(stream))
					{
						writer.Write((byte)0xFE);
						writer.Write((byte)_payloadLength);
						writer.Write(_packageSequence++);
						writer.Write(MavLink.SystemId);
						writer.Write(MavLink.ComponentId);
						writer.Write(_messageId);

						for (int i = 0; i < _fields.Length; ++i)
						{
							WriteField(writer, i);
						}

						// Append two bytes to be replaced by the CRC later on
						writer.Write((byte)0);
						writer.Write((byte)0);
						writer.Flush();
					}

					data = stream.ToArray();
				}

				MavLinkCRC crc = new MavLinkCRC();
				for (int i = 1; i < data.Length - 2; ++i)
				{
					crc.update_checksum(data[i]);
				}
				crc.finish_checksum(_messageId);

				data[data.Length - 2] = (byte)crc.getLSB();
				data[data.Length - 1] = (byte)crc.getMSB();

				return data;
			}

			public static MessageBase Deserialize(byte[] data)
			{
				using (MemoryStream stream = new MemoryStream(data))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					// Ignore initial key
					reader.ReadByte();
					byte payloadLength = reader.ReadByte();
					byte packageSequence = reader.ReadByte();
					byte systemId = reader.ReadByte();
					byte componentId = reader.ReadByte();
					byte messageId = reader.ReadByte();

					// Create instance based on message id
					MessageBase message = null;
					Type messageType;
					if (_messageTypes.TryGetValue(messageId, out messageType))
					{
						message = (MessageBase)Activator.CreateInstance(messageType);

						message._fields = new object[message._fieldTypes.Length];
						for (int i = 0; i < message._fieldTypes.Length; ++i)
						{
							message.ReadField(reader, i);
						}

						// TODO: Verify CRC
						byte LSBCRC = reader.ReadByte();
						byte MSBCRC = reader.ReadByte();
					}

					return message;
				}
			}

			public override bool Equals(object obj)
			{
				MessageBase other = obj as MessageBase;
				if (other == null)
					return base.Equals(obj);

				if (_fields.Length != other._fields.Length)
					return false;

				for (int i = 0; i < _fields.Length; ++i)
				{
					if (_arrayLengths[i] > 0)
					{
						Array left = (Array)_fields[i];
						Array right = (Array)other._fields[i];

						if (left.Length != right.Length)
							return false;

						for (int j = 0; j < left.Length; ++j)
						{
							if (left.GetValue(j) != right.GetValue(j))
								return false;
						}
					}
					else if (_fields[i] != other._fields[i])
						return false;
				}

				return true;
			}

			public override int GetHashCode()
			{
				int hash = 0;
				for (int i = 0; i < _fields.Length; ++i)
				{
					if (_arrayLengths[i] > 0)
					{
						Array array = (Array)_fields[i];

						for (int j = 0; j < array.Length; ++j)
						{
							hash ^= array.GetValue(j).GetHashCode();
						}
					}
					else
						hash ^= _fields[i].GetHashCode();
				}

				return hash;
			}

			protected virtual void OnPropertyChanged(string propertyName)
			{
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion

		#region Messages

		/*MESSAGES*/

		#endregion
	}
}
