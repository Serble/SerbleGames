using System;
using System.Buffers.Binary;
using System.Collections;
using System.IO;
using System.Text;

namespace LauncherGodot.Scripts;

public class DataReader(byte[] data) : Stream {
    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _data.Length;

    public override long Position {
        get => Pos;
        set => Pos = (int)value;
    }
    
    internal int Pos;
    private byte[] _data = data;
    public bool HasRemaining => Pos < _data.Length;

    public void UpdateData(byte[] newData) {
        _data = newData;
    }

    public byte Read() {
        return Read(1)[0];
    }

    public byte[] Read(uint bytes) => Read((int)bytes);
    
    public byte[] Read(int bytes) {
        if (Pos + bytes > _data.Length) {
            throw new Exception("Reached the end of the data.");
        }

        byte[] newData = _data[Pos..(Pos + bytes)];
        Pos += bytes;
        return newData;
    }

    public T ReadPrefixedOptional<T>(Func<DataReader, T> reader) {
        bool exists = ReadBoolean();
        return exists ? reader(this) : default;
    }

    public string ReadString() {
        int length = ReadInteger();
        return Encoding.UTF8.GetString(Read(length));
    }
    
    public ushort ReadUShort() {
        byte[] bytes = Read(2);
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }
    
    public long ReadLong() {
        byte[] bytes = Read(8);
        long value = 0;
        for (int i = 0; i < 8; i++) {
            value |= (long)(bytes[i] & 0xFF) << (56 - i * 8); // Big-endian
        }
        return value;
    }

    public short ReadShort() {
        byte[] bytes = Read(2);
        return BinaryPrimitives.ReadInt16BigEndian(bytes);
    }

    public int ReadInteger() {
        byte[] bytes = Read(4);
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }

    public uint ReadUInteger() {
        return BinaryPrimitives.ReadUInt32BigEndian(Read(4));
    }
    
    public float ReadFloat() {
        byte[] bytes = Read(4);
        if (BitConverter.IsLittleEndian) {
            Array.Reverse(bytes);
        }
        return BitConverter.ToSingle(bytes, 0);
    }
    
    public double ReadDouble() {
        byte[] bytes = Read(8);
        if (BitConverter.IsLittleEndian) {
            Array.Reverse(bytes);
        }
        return BitConverter.ToDouble(bytes, 0);
    }

    public bool ReadBoolean() {
        byte[] bytes = Read(1);
        return bytes[0] != 0;
    }
    
    // Sign-extension helper for bits-bit two's complement
    private static int SignExtend(int value, int bits) {
        int shift = 32 - bits; 
        return (value << shift) >> shift;
    }
    
    // From an N-bit integer represented as a BitArray in big-endian order.
    public static ushort FromNBitInteger(int bits, BitArray bitArr) {
        if (bitArr.Count != bits) {
            throw new ArgumentOutOfRangeException(nameof(bitArr), $"Data must be {nameof(bits)} long.");
        }
        
        ushort value = 0;
        for (int i = 0; i < bits; i++) {
            if (bitArr[bits - i - 1]) {
                value |= (ushort)(1 << i);
            }
        }
        
        return value;
    }
    
    // reads a signed 8-bit integer (two's complement)
    public new sbyte ReadByte() {
        byte b = Read(1)[0];
        if (b >= 128) {
            // Convert to two's complement for negative values
            return (sbyte)(b - 256); // 256 - 128 = 128, so -128 becomes 128
        }
        return (sbyte)(b & 0xFF);
    }

    public Guid ReadUuid() {
        return new Guid(Read(16), true);
    }

    public T[] ReadArray<T>(int length, Func<DataReader, T> readerAdapter) {
        T[] arr = new T[length];
        for (int i = 0; i < length; i++) {
            arr[i] = readerAdapter.Invoke(this);
        }
        return arr;
    }
    
    public T[] ReadArray<T>(int length, Func<DataReader, int, T> readerAdapter) {
        T[] arr = new T[length];
        for (int i = 0; i < length; i++) {
            arr[i] = readerAdapter.Invoke(this, i);
        }
        return arr;
    }
    
    public byte[] ReadRemaining() {
        return _data[Pos..];
    }
    
    public override void Flush() {
        // No-op for DataReader, as it does not write data
    }

    public override int Read(byte[] buffer, int offset, int count) {
        if (Pos + count > _data.Length) {
            count = _data.Length - Pos;  // Adjust count to not exceed the data length
        }

        Array.Copy(_data, Pos, buffer, offset, count);
        Pos += count;
        return count;  // Return the number of bytes read
    }

    public override long Seek(long offset, SeekOrigin origin) {
        switch (origin) {
            case SeekOrigin.Begin:
                Pos = (int)offset;
                break;
            case SeekOrigin.Current:
                Pos += (int)offset;
                break;
            case SeekOrigin.End:
                Pos = _data.Length + (int)offset;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }

        if (Pos < 0 || Pos > _data.Length) {
            throw new ArgumentOutOfRangeException(nameof(offset), "Seek position is out of bounds.");
        }

        return Pos;
    }

    public override void SetLength(long value) {
        if (value < 0 || value > _data.Length) {
            throw new ArgumentOutOfRangeException(nameof(value), "Length must be within the bounds of the data.");
        }
        
        // Adjust the internal data array if necessary
        if (value < _data.Length) {
            Array.Resize(ref _data, (int)value);
        } else if (value > _data.Length) {
            byte[] newData = new byte[value];
            Array.Copy(_data, newData, _data.Length);
            _data = newData;
        }
    }

    public override void Write(byte[] buffer, int offset, int count) {
        throw new NotSupportedException("DataReader does not support writing data.");
    }
}
