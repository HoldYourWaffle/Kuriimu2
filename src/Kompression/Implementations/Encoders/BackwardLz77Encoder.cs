﻿using System;
using System.Collections.Generic;
using System.IO;
using Kompression.IO;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    public class BackwardLz77Encoder : IPatternMatchEncoder
    {
        private readonly ByteOrder _byteOrder;
        private byte _codeBlock;
        private int _codeBlockPosition;
        private byte[] _buffer;
        private int _bufferLength;

        public BackwardLz77Encoder(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public void Encode(Stream input, Stream output, Match[] matches)
        {
            // Displacement goes to the end of the file relative to the match position
            // Length goes to the beginning of the file relative to the match position

            var compressedLength = PrecalculateCompressedLength(input.Length, matches);

            _codeBlock = 0;
            _codeBlockPosition = 8;
            // We write all data backwards into the buffer; starting from last element down to first
            // We have 8 blocks; A block can be at max 2 bytes, defining a match
            _buffer = new byte[8 * 2];
            _bufferLength = 0;

            using (var reverseInputStream = new ReverseStream(input, input.Length))
            using (var reverseOutputStream = new ReverseStream(output, compressedLength))
            {
                foreach (var match in matches)
                {
                    while (match.Position > reverseInputStream.Position)
                    {
                        if (_codeBlockPosition == 0)
                            WriteAndResetBuffer(reverseOutputStream);

                        _codeBlockPosition--;
                        _buffer[_bufferLength++] = (byte)reverseInputStream.ReadByte();
                    }

                    var byte1 = ((byte)(match.Length - 3) << 4) | (byte)((match.Displacement - 3) >> 8);
                    var byte2 = match.Displacement - 3;

                    if (_codeBlockPosition == 0)
                        WriteAndResetBuffer(reverseOutputStream);

                    _codeBlock |= (byte)(1 << --_codeBlockPosition);
                    _buffer[_bufferLength++] = (byte)byte1;
                    _buffer[_bufferLength++] = (byte)byte2;

                    reverseInputStream.Position += match.Length;
                }

                // Write any data after last match, to the buffer
                while (reverseInputStream.Position < reverseInputStream.Length)
                {
                    if (_codeBlockPosition == 0)
                        WriteAndResetBuffer(reverseOutputStream);

                    _codeBlockPosition--;
                    _buffer[_bufferLength++] = (byte)reverseInputStream.ReadByte();
                }

                // Flush remaining buffer to stream
                WriteAndResetBuffer(reverseOutputStream);

                output.Position = compressedLength;
                WriteFooterInformation(input, output);
            }
        }

        private int PrecalculateCompressedLength(long uncompressedLength, IEnumerable<Match> matches)
        {
            var length = 0;
            var writtenCodes = 0;
            foreach (var match in matches)
            {
                var rawLength = uncompressedLength - match.Position - 1;

                // Raw data before match
                length += (int)rawLength;
                writtenCodes += (int)rawLength;
                uncompressedLength -= rawLength;

                // Match data
                writtenCodes++;
                length += 2;
                uncompressedLength -= match.Length;
            }

            length += (int)uncompressedLength;
            writtenCodes += (int)uncompressedLength;

            length += writtenCodes / 8;
            length += writtenCodes % 8 > 0 ? 1 : 0;

            return length;
        }

        private void WriteFooterInformation(Stream input, Stream output)
        {
            // Remember count of padding bytes
            var padding = 0;
            if (output.Length % 4 != 0)
                padding = (int)(4 - output.Position % 4);

            // Write padding
            for (var i = 0; i < padding; i++)
                output.WriteByte(0xFF);

            // Write footer
            var compressedSize = output.Position + 8;
            var bufferTopAndBottomInt = ((8 + padding) << 24) | (int)(compressedSize & 0xFFFFFF);
            var originalBottomInt = (int)(input.Length - compressedSize);

            var bufferTopAndBottom = _byteOrder == ByteOrder.LittleEndian
                ? GetLittleEndian(bufferTopAndBottomInt)
                : GetBigEndian(bufferTopAndBottomInt);
            var originalBottom = _byteOrder == ByteOrder.LittleEndian
                ? GetLittleEndian(originalBottomInt)
                : GetBigEndian(originalBottomInt);
            output.Write(bufferTopAndBottom, 0, 4);
            output.Write(originalBottom, 0, 4);
        }

        private void WriteAndResetBuffer(Stream output)
        {
            // Write data to output
            output.WriteByte(_codeBlock);
            output.Write(_buffer, 0, _bufferLength);

            // Reset codeBlock and buffer
            _codeBlock = 0;
            _codeBlockPosition = 8;
            Array.Clear(_buffer, 0, _bufferLength);
            _bufferLength = 0;
        }

        private byte[] GetLittleEndian(int value)
        {
            return new[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
        }

        private byte[] GetBigEndian(int value)
        {
            return new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
        }

        public void Dispose()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _buffer = null;
        }
    }
}
