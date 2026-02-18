using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LauncherGodot.Scripts;

public class DataWriter : Stream {
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _data?.Count ?? _stream!.Length;

    public override long Position {
        get => _data?.Count ?? _stream!.Position;
        set => throw new NotSupportedException();
    }

    private readonly List<byte>? _data;
    private readonly Stream? _stream;

    public byte[] ToArray() => _data?.ToArray()!;
    
    public List<byte>? GetRaw() => _data;


    public DataWriter() {
        _data = [];
    }
    
    public DataWriter(Stream stream) {
        _stream = stream;
    }
    
    public DataWriter Write(byte[] value) {
        if (_data == null) {
            _stream!.Write(value);
        }
        else {
            _data.AddRange(value);
        }
        return this;
    }
    
    public DataWriter Write(IEnumerable<byte> value) {
        if (_data == null) {
            foreach (byte b in value) {
                _stream!.WriteByte(b);
            }
        }
        else {
            _data.AddRange(value);
        }
        return this;
    }
    
    public DataWriter Write(Stream stream) {
        if (_data == null) {
            stream.CopyTo(_stream!);
        }
        else {
            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0) {
                _data.AddRange(buffer.AsSpan(0, bytesRead).ToArray());
            }
        }
        return this;
    }

    public DataWriter Write(byte value) {
        if (_data == null) {
            _stream!.WriteByte(value);
        }
        else {
            _data.Add(value);
        }
        return this;
    }

    /// <summary>
    /// Write this DataWriter's data to the given DataWriter.
    /// </summary>
    /// <param name="w"></param>
    public void Write(DataWriter w) {
        if (_data == null) {
            w.Write(_stream!);
        }
        else {
            foreach (byte b in _data) {
                w.Write(b);
            }
        }
    }

    public DataWriter Write(Func<DataWriter, DataWriter> writeAction) {
        return writeAction(this);
    }

    public DataWriter Write(Action<DataWriter> writeAction) {
        writeAction(this);
        return this;
    }
    
    public DataWriter Write<T>(T val, Action<T, DataWriter> writeAction) {
        writeAction(val, this);
        return this;
    }
    
    // A single-precision 32-bit IEEE 754 floating point number, big endian
    public DataWriter WriteFloat(float value) {
        byte[] buffer = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) {
            Array.Reverse(buffer); // Convert to big-endian
        }
        return Write(buffer);
    }
    
    // Signed 32-bit integer, two's complement
    public DataWriter WriteInteger(int value) {
        byte[] buffer = new byte[4];
        buffer[0] = (byte)(value >> 24); // High byte first (big-endian)
        buffer[1] = (byte)(value >> 16);
        buffer[2] = (byte)(value >> 8);
        buffer[3] = (byte)(value & 0xFF); // Low byte last
        return Write(buffer);
    }
    
    // A double-precision 64-bit IEEE 754 floating point number, big endian
    public DataWriter WriteDouble(double value) {
        byte[] buffer = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) {
            Array.Reverse(buffer); // Convert to big-endian
        }
        return Write(buffer);
    }

    public DataWriter WriteString(string value) {
        byte[] bs = Encoding.UTF8.GetBytes(value);
        WriteInteger(bs.Length);
        Write(bs);
        return this;
    }

    public DataWriter WriteUShort(ushort value) {
        byte[] buffer = new byte[2];
        buffer[0] = (byte)(value >> 8);      // High byte first (big-endian)
        buffer[1] = (byte)(value & 0xFF);    // Low byte second
        return Write(buffer);
    }
    
    // Signed 16-bit integer, two's complement
    public DataWriter WriteShort(short value) {
        Span<byte> span = new(new byte[sizeof(short)]);
        BinaryPrimitives.WriteInt16BigEndian(span, value);
        return Write(span.ToArray());
    }
    
    // writes a signed 64-bit integer (two's complement)
    public DataWriter WriteLong(long value) {
        byte[] buffer = new byte[8];
        for (int i = 0; i < 8; i++) {
            buffer[i] = (byte)((value >> (56 - i * 8)) & 0xFF); // Big-endian
        }
        return Write(buffer);
    }
    
    // To an N-bit integer represented as a BitArray in big-endian order.
    public static BitArray ToNBitInteger(int bits, ushort value) {
        if (bits is < 1 or > 64) {
            throw new ArgumentOutOfRangeException(nameof(bits), "Bits must be between 1 and 64.");
        }
        
        BitArray bitArray = new(bits);
        for (int i = 0; i < bits; i++) {
            // Set the bit at position i to the corresponding bit in value
            // The least significant bit is at index 0, so we use (bits - 1 - i) to reverse the order
            bitArray[bits - 1 - i] = (value & (1L << i)) != 0;
            // bitArray[i] = (value & (1L << i)) != 0;
        }

        return bitArray;
    }
    
    public static long[] PackToLongArray(int bitsPerEntry, ushort[] data) {
        double intsPerLong = Math.Floor(64d / bitsPerEntry);
        int intsPerLongCeil = (int)Math.Ceiling(intsPerLong);
        
        int longCount = (int)Math.Ceiling((double)data.Length / intsPerLongCeil);
        long[] outp = new long[longCount];

        long mask = (1L << bitsPerEntry) - 1L;
        for (int i = 0; i < data.Length; i++) {
            int longIndex = i / intsPerLongCeil;
            int subIndex = i % intsPerLongCeil;

            outp[longIndex] |= (data[i] & mask) << (bitsPerEntry * subIndex);
        }

        return outp;
    }
    
    // writes a signed 8-bit integer (two's complement)
    public DataWriter WriteByte(int value) {
        if (value is < -128 or > 127) {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be between -128 and 127.");
        }

        if (value >= 0) {
            return Write((byte)value);
        }
        
        // Convert to two's complement for negative values
        return Write((byte)(256 + value)); // 256 - 128 = 128, so -128 becomes 128
    }
    
    // unsigned 8-bit integer
    public DataWriter WriteUnsignedByte(byte value) {
        return Write(value);
    }

    public DataWriter WriteBoolean(bool value) {
        return Write((byte)(value ? 0x01 : 0x00));
    }

    public DataWriter WritePrefixedOptional<T>(T? value, Action<T, DataWriter> writer) where T : class {
        if (value == null) {
            return WriteBoolean(false);
        }

        WriteBoolean(true);
        writer.Invoke(value, this);
        return this;
    }
    
    public DataWriter WritePrefixedOptional<T>(T? value, Action<T, DataWriter> writer) where T : struct {
        if (!value.HasValue) {
            return WriteBoolean(false);
        }

        WriteBoolean(true);
        writer.Invoke(value.Value, this);
        return this;
    }

    public DataWriter WriteIfPresent<T>(T value, Action<T, DataWriter> writer) where T : class {
        if (value == null) {
            return this; // Do nothing if not present
        }

        writer.Invoke(value, this);
        return this;
    }
    
    public DataWriter WriteIfPresent<T>(T? value, Action<T, DataWriter> writer) where T : struct {
        if (!value.HasValue) {
            return this; // Do nothing if not present
        }

        writer.Invoke(value.Value, this);
        return this;
    }

    public DataWriter WriteUuid(Guid value) {
        return Write(value.ToByteArray(true));
    }

    public DataWriter WriteArray<T>(IEnumerable<T> values, Action<T, DataWriter> writerAction) {
        foreach (T value in values) {
            writerAction.Invoke(value, this);
        }
        return this;
    }
    
    public override void Flush() {
        // No-op for DataReader, as it does not write data
    }

    public override int Read(byte[] buffer, int offset, int count) {
        throw new NotSupportedException("DataWriter does not support reading data.");
    }

    public override long Seek(long offset, SeekOrigin origin) {
        throw new NotSupportedException("DataWriter does not support seeking.");
    }

    public override void SetLength(long value) {
        throw new NotSupportedException("DataWriter does not support setting length.");
    }

    public override void Write(byte[] buffer, int offset, int count) {
        if (buffer == null) {
            throw new ArgumentNullException(nameof(buffer), "Buffer cannot be null.");
        }
        
        if (offset < 0 || count < 0 || offset + count > buffer.Length) {
            throw new ArgumentOutOfRangeException(nameof(count), "Offset and count must be within the bounds of the buffer.");
        }

        for (int i = offset; i < offset + count; i++) {
            Write(buffer[i]);
        }
    }
}
