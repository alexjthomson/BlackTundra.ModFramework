using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

using UnityEngine;

namespace BlackTundra.ModFramework.Media {

    /// <summary>
    /// Parses WAV files to an <see cref="AudioClip"/>.
    /// </summary>
    public static class WavImporter {

        #region constant

        private static readonly ASCIIEncoding Encoding = new ASCIIEncoding();

        private static readonly byte[] RIFFHeaderChunkID = Encoding.GetBytes("RIFF");
        private static readonly byte[] RIFFHeaderExpectedFileType = Encoding.GetBytes("WAVE");
        private static readonly byte[] FMT_SubchunkID = Encoding.GetBytes("fmt ");
        private static readonly byte[] DATASubchunkID = Encoding.GetBytes("data");

        private const float _8BitPerSampleCoefficient = 1.0f / 128.0f;
        private const float _16BitPerSampleCoefficient = 1.0f / 32768.0f;
        private const float _24BitPerSampleCoefficient = 1.0f / 8388608.0f;

        #endregion

        #region enum

        private enum WaveFormat : ushort {
            PCM = 0x0001,
            IEEE_FLOAT = 0x0003,
            ALAW = 0x0006,
            MULAW = 0x0007,
            EXTENSIBLE = 0xFFFE
        }

        #endregion

        #region logic

        public static AudioClip Import(in string name, in FileSystemReference fsr) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (!fsr.IsFile) throw new ArgumentException($"{nameof(fsr)} must reference a file.");
            // read wav data:
            if (!FileSystem.Read(fsr, out byte[] bytes, FileFormat.Standard)) {
                throw new IOException($"Failed to read wav file at `{fsr}`.");
            }

            // WAVE Format Documentation & Specification:
            // http://soundfile.sapp.org/doc/WaveFormat/

            /*
             * -------------------------------------------------- RIFF HEADER --------------------------------------------------
             * 0    4   ChunkID         Contains the letters "RIFF" in big endian ASCII format
             * 4    4   ChunkSize       4 + (8 + Subchunk1Size) + (8 + Subchunk2Size)
             * 8    4   Format          Contains the letters "WAVE" in big endian ASCII format
             */

            // ChunkID:
            if (!bytes.ContentEquals(0, RIFFHeaderChunkID, 0, 4)) throw new FormatException("Invalid RIFF header chunk ID.");

            // ChunkSize:
            uint chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..8]);
            // The ChunkSize is verified later using the equation in the above ChunkSize definition

            // Format:
            if (!bytes.ContentEquals(8, RIFFHeaderExpectedFileType, 0, 4)) throw new FormatException("Invalid RIFF header format field.");

            /*
             * ------------------------------------------------- FMT_ SUBCHUNK -------------------------------------------------
             * This sub-chunk describes the sound data's format:
             * 12   4   Subchunk1ID     Contains the letters "RIFF" in big endian ASCII format
             * 16   4   Subchunk1Size   16 for PCM
             * 20   2   AudioFormat     PCM = 1 (i.e. linear quantization). Values other than 1 indicate some form of compression
             * 22   2   NumChannels     Mono = 1, Stereo = 2, etc.
             * 24   4   SampleRate      8000, 44100, etc.
             * 28   4   ByteRate        == SampleRate * NumChannels * BitsPerSample/8
             * 32   2   BlockAlign      == NumChannels * BitsPerSample/8
             *                          The number of bytes for one sample including all channels.
             * 34   2   BitsPerSample   8 bits = 8, 16 bits = 16, etc.
             *      2   ExtraParamSize  if PCM then this does not exist
             *      x   ExtraParams     Space for extra parameters
             */

            // Subchunk1ID:
            if (!bytes.ContentEquals(12, FMT_SubchunkID, 0, 4)) throw new FormatException("Invalid FMT_ subchunk ChunkID.");

            // Subchunk1Size:
            uint subchunk1Size = BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..20]);

            // AudioFormat:
            ushort audioFormatCode = BinaryPrimitives.ReadUInt16LittleEndian(bytes[20..22]);
            if (!((ushort[])Enum.GetValues(typeof(WaveFormat))).Contains(audioFormatCode)) {
                throw new NotSupportedException($"Unsupported FMT_ subchunk WAVE format `{audioFormatCode}`.");
            }
            WaveFormat audioFormat = (WaveFormat)audioFormatCode;

            // NumChannels:
            ushort numChannels = BinaryPrimitives.ReadUInt16LittleEndian(bytes[22..24]);
            if (numChannels == 0) throw new FormatException("Invalid FMT_ subchunk NumChannels is zero.");

            // SampleRate:
            int sampleRate = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[24..28]);

            // ByteRate:
            uint byteRate = BinaryPrimitives.ReadUInt32LittleEndian(bytes[28..32]);

            // BlockAlign:
            ushort blockAlign = BinaryPrimitives.ReadUInt16LittleEndian(bytes[32..34]);

            // BitsPerSample:
            ushort bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(bytes[34..36]);
            ushort bytesPerSample = (ushort)(bitsPerSample / 8); // calculate the number of bytes per sample

            // Verify BlockAlign:
            ushort expectedBlockAlign = (ushort)(numChannels * bytesPerSample);
            if (blockAlign != expectedBlockAlign) throw new FormatException("Unexpected FMT_ subchunk BlockAlign value.");

            // Verify ByteRate:
            int expectedByteRate = sampleRate * expectedBlockAlign;
            if (byteRate != expectedByteRate) throw new FormatException("Unexpected FMT_ subchunk ByteRate value.");

            /*
             * This is the end of the FMT_ subchunk. Some applications may place additional data at the end of this
             * sub-chunk. Here we are checking to see if there is any additional data since the expected length of
             * this sub-chunk is 16 and we are given the actual size of the sub-chunk already. We can find the
             * difference to calculate the number of additional bytes of data at the end of this sub-chunk that can
             * be discarded as additional data.
             */
            int subchunk1AdditionalBytes = (int)(subchunk1Size - 16);

            /*
             * ------------------------------------------------- DATA SUBCHUNK -------------------------------------------------
             * This sub-chunk contains the size of the data and the actual audio:
             * Let X = the number of additional bytes found in the FMT_ subchunk. This can be thought of as an offset.
             * 36+X 4   Subchunk2ID     Contains the letters "data" in big endian ASCII format
             * 40+X 4   Subchunk2Size   == NumSamples * NumChannels * BitsPerSample/8
             *                          This is the number of bytes in the data. You can also think of this as the size of the
             *                          read of the subchunk following this number.
             * 44+X *   Data            The actual sound data.
             */

            /*
             * Here we can calculate the start index for the samples of actual audio based off of the offset from the FMT_ sub-chunk:
             */
            int sampleStartIndex = 44 + subchunk1AdditionalBytes;

            // Subchunk2ID:
            if (!bytes.ContentEquals(36 + subchunk1AdditionalBytes, DATASubchunkID, 0, 4)) throw new FormatException("Invalid DATA subchunk ChunkID.");

            // Subchunk2Size:
            uint subchunk2Size = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(40 + subchunk1AdditionalBytes)..sampleStartIndex]);

            /*
             * DO NOT VERIFY CHUNK SIZE!
             * There appears to be some kind of padding or rubbish at the end of the file causing this to fail with 90% of files.
             * 
            
            // Verify ChunkSize:
            uint expectedChunkSize = 4 + (8 + subchunk1Size) + (8 + subchunk2Size);
            Debug.Log($"{fsr.FileName} {(chunkSize - expectedChunkSize)} {audioFormat} 1:{subchunk1Size} 2:{subchunk2Size} additional space: {subchunk1AdditionalBytes}");
            if (chunkSize != expectedChunkSize) throw new FormatException($"Unexpected RIFF header ChunkSize (actual: `{chunkSize}`, expected: `{expectedChunkSize}`).");
            
             */

            /*
             * Here we can calculate the number of samples from the Subchunk2Size:
             * Since the Subchunk2Size is equal to `NumSamples * NumChannels * BitsPerSample/8` we can reverse engineer this to
             * find NumSamples:
             * 
             * Subchunk2Size / (NumChannels * BitsPerSample/8) == NumSamples
             */
            int numSamples = (int)(subchunk2Size / blockAlign);

            /*
             * ----------------------------------------------- AudioClip Samples -----------------------------------------------
             * The next Subchunk2Size number of bytes are all audio data. This data is formatted slightly differently depending
             * on the number of bits/bytes per sample:
             * BITS/SAMPLE      DESCRIPTION
             * 8                Stored as unsigned BYTEs ranging from 0 to 255.
             * 16               Stored as signed SHORTs ranging from -32768 to 32767
             * 24               Same as 16 bit but using 3 bytes
             * 32               Stored as 32BIT FLOATING POINT VALUES
             */
            int sampleCount = numSamples * numChannels;
            float[] samples = new float[sampleCount];
            switch (audioFormat) {
                case WaveFormat.PCM: {
                    switch (bitsPerSample) {
                        case 8: { // ubyte
                            for (int i = 0; i < sampleCount; i++) { // iterate each sample
                                samples[i] = (bytes[sampleStartIndex + i] - 128) * _8BitPerSampleCoefficient;
                            }
                            break;
                        }
                        case 16: { // short
                            int sampleIndex;
                            for (int i = 0; i < sampleCount; i++) {
                                sampleIndex = sampleStartIndex + (i * 2);
                                samples[i] = BinaryPrimitives.ReadInt16LittleEndian(bytes[sampleIndex..(sampleIndex + 2)]) * _16BitPerSampleCoefficient;
                            }
                            break;
                        }
                        case 24: { // 3 signed bytes
                            int sampleIndex;
                            for (int i = 0; i < sampleCount; i++) {
                                sampleIndex = sampleStartIndex + (i * 3);
                                samples[i] = ((bytes[sampleIndex + 2] << 24 | bytes[sampleIndex + 1] << 16 | bytes[sampleIndex] << 8) >> 8) * _24BitPerSampleCoefficient;
                            }
                            break;
                        }
                        case 32: { // 32bit signed floating point number (float)
                            if (BitConverter.IsLittleEndian) {
                                for (int i = 0; i < sampleCount; i++) {
                                    samples[i] = BitConverter.ToSingle(bytes, sampleStartIndex + (i * 4));
                                }
                            } else {
                                int sampleIndex;
                                for (int i = 0; i < sampleCount; i++) {
                                    sampleIndex = sampleStartIndex + (i * 4);
                                    samples[i] = BitConverter.ToSingle(
                                        BitConverter.GetBytes(
                                            BinaryPrimitives.ReadUInt32BigEndian(
                                                bytes[sampleIndex..(sampleIndex + 4)]
                                            )
                                        ),
                                        0
                                    );
                                }
                            }
                            break;
                        }
                        default: throw new NotSupportedException($"{bitsPerSample} bits per sample is not supported for WAVE format imports.");
                    }
                    break;
                }
                case WaveFormat.IEEE_FLOAT: {
                    switch (bitsPerSample) {
                        case 32: { // 32bit signed floating point number (float)
                            if (BitConverter.IsLittleEndian) {
                                for (int i = 0; i < sampleCount; i++) {
                                    samples[i] = BitConverter.ToSingle(bytes, sampleStartIndex + (i * 4));
                                }
                            } else {
                                int sampleIndex;
                                for (int i = 0; i < sampleCount; i++) {
                                    sampleIndex = sampleStartIndex + (i * 4);
                                    samples[i] = BitConverter.ToSingle(
                                        BitConverter.GetBytes(
                                            BinaryPrimitives.ReadUInt32BigEndian(
                                                bytes[sampleIndex..(sampleIndex + 4)]
                                            )
                                        ),
                                        0
                                    );
                                }
                            }
                            break;
                        }
                        case 64: { // 64bit signed floating point number (double)
                            if (BitConverter.IsLittleEndian) {
                                for (int i = 0; i < sampleCount; i++) {
                                    samples[i] = (float)BitConverter.ToDouble(bytes, sampleStartIndex + (i * 4));
                                }
                            } else {
                                int sampleIndex;
                                for (int i = 0; i < sampleCount; i++) {
                                    sampleIndex = sampleStartIndex + (i * 8);
                                    samples[i] = (float)BitConverter.ToDouble(
                                        BitConverter.GetBytes(
                                            BinaryPrimitives.ReadUInt64BigEndian(
                                                bytes[sampleIndex..(sampleIndex + 8)]
                                            )
                                        ),
                                        0
                                    );
                                }
                            }
                            break;
                        }
                        default: throw new NotSupportedException($"{bitsPerSample} bits per sample is not supported for WAVE format imports.");
                    }
                    break;
                }
                default: throw new NotSupportedException($"Wave format `{audioFormat}` not supported.");
            }

            // create audio clip:

            AudioClip audioClip = AudioClip.Create(name, numSamples, numChannels, sampleRate, false);
            audioClip.SetData(samples, 0);
            Debug.Log($"success: {audioFormat} {name} {fsr.FileName}");
            return audioClip;
        }

        #endregion

    }

}