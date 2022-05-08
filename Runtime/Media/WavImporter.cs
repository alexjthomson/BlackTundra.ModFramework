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

        private static readonly byte[] RIFF_HeaderChunkID = Encoding.GetBytes("RIFF");
        private static readonly byte[] RIFF_HeaderExpectedFileType = Encoding.GetBytes("WAVE");

        private const float _8BitPerSampleCoefficient = 1.0f / 128.0f;
        private const float _16BitPerSampleCoefficient = 1.0f / 32768.0f;
        private const float _24BitPerSampleCoefficient = 1.0f / 8388608.0f;
        private const float _32BitPerSampleCoefficient = 1.0f / 2147483648.0f;

        #endregion

        #region enum

        /// <summary>
        /// Describes the format of some WAVE data.
        /// </summary>
        private enum WaveAudioFormat : ushort {
            /// <summary>
            /// Unknown format, used as a placeholder.
            /// </summary>
            Unknown = 0x0000,

            /// <summary>
            /// Short for `Pulse Code Modulation`, PCM is a method to capture waveforms and store the audio digitally. This
            /// format simply means raw PCM data is the payload in the WAV data.
            /// </summary>
            PCM = 0x0001,

            /// <summary>
            /// Short for `Institute of Electrical and Electronic Engineers Floating point number`, IEEE_FLOAT simply means
            /// instead of storing raw PCM data as integers, it is instead stored as either 32bit or 64bit floating point
            /// numbers.
            /// </summary>
            IEEE_FLOAT = 0x0003,

            ALAW = 0x0006, MULAW = 0x0007, // UNSUPPORTED

            /// <summary>
            /// This is a format that indicates there is an extension to the FMT_ sub-chunk. The extension has one field
            /// (BitsPerSample) which declares the number of "valid" bits per sample. Another field (ChannelMask) contains
            /// bits which indicate the mapping from channels to loudspeaker positions. The last field (SubFormat) is a
            /// 16-byte globally unique identifier (GUID).
            /// </summary>
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

            /*
             * TODO:
             * - Finish implementing the EXTENSIBLE format by adding it to the final switch statement where the
             *   audio payload is processed.
             */

            // WAVE Format Documentation & Specifications:
            // http://soundfile.sapp.org/doc/WaveFormat/
            // http://www-mmsp.ece.mcgill.ca/Documents/AudioFormats/WAVE/WAVE.html
            // https://tech.ebu.ch/docs/tech/tech3306v1_0.pdf

            /*
             * -------------------------------------------------- RIFF HEADER --------------------------------------------------
             * 0    4   ChunkID         Contains the letters "RIFF" in big endian ASCII format
             * 4    4   ChunkSize       4 + (8 + Subchunk1Size) + (8 + Subchunk2Size)
             * 8    4   Format          Contains the letters "WAVE" in big endian ASCII format
             */

            // ChunkID:
            if (!bytes.ContentEquals(0, RIFF_HeaderChunkID, 0, 4)) throw new FormatException("Invalid RIFF header chunk ID.");

            // ChunkSize:
            uint chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..8]);
            int dataSize = (int)(chunkSize + 8);

            // Format:
            if (!bytes.ContentEquals(8, RIFF_HeaderExpectedFileType, 0, 4)) throw new FormatException("Invalid RIFF header format field.");

            /*
             * ------------------------------------------------ PARSE SUBCHUNKS ------------------------------------------------
             * Subchunks exist within the RIFF chunk. These chunks contain useful meta-data about the WAVE data. These
             * sub-chunks may appear in any order. There may also be propritary sub-chunks depending on the software used to
             * produce the WAVE data.
             */

            int offset = 12; // create offset to use to track the current position in the byte array
            bool containsFormatSubchunk = false;
            bool containsFactSubchunk = false;

            WaveAudioFormat audioFormat = WaveAudioFormat.Unknown;
            ushort numChannels = 0;
            int sampleRate = -1;
            ushort blockAlign = 0;
            ushort bitsPerSample = 0;
            int numSamples = 0;

            // begin parsing sub-chunks:
            while (offset < dataSize) {
                // get sub-chunk id:
                string subchunkId = Encoding.GetString(bytes[offset..(offset + 4)]);

                // get sub-chunk size:
                uint subchunkSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 4)..(offset + 8)]);

                // process sub-chunk:
                switch (subchunkId) {
                    case "fmt ": { // format sub-chunk
                        /*
                         * ------------------------------------------------- FMT_ SUBCHUNK -------------------------------------------------
                         * This sub-chunk describes the sound data's format:
                         * 0    4   Subchunk1ID     Contains the letters "fmt " in big endian ASCII format
                         * 4    4   Subchunk1Size   16 for PCM
                         * 8    2   AudioFormat     PCM = 1 (i.e. linear quantization). Values other than 1 indicate some form of compression
                         * 10   2   NumChannels     Mono = 1, Stereo = 2, etc.
                         * 12   4   SampleRate      8000, 44100, etc.
                         * 16   4   ByteRate        == SampleRate * NumChannels * BitsPerSample/8
                         * 20   2   BlockAlign      == NumChannels * BitsPerSample/8
                         *                          The number of bytes for one sample including all channels.
                         * 22   2   BitsPerSample   8 bits = 8, 16 bits = 16, etc.
                         * 24   2   ExtraParamSize  if PCM then this does not exist
                         * 26   x   ExtraParams     Space for extra parameters
                         */
                        containsFormatSubchunk = true;

                        // AudioFormat:
                        ushort audioFormatCode = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(offset + 8)..(offset + 10)]);
                        if (!((ushort[])Enum.GetValues(typeof(WaveAudioFormat))).Contains(audioFormatCode)) {
                            throw new NotSupportedException($"Unsupported FMT_ subchunk WAVE format `{audioFormatCode}`.");
                        }
                        audioFormat = (WaveAudioFormat)audioFormatCode;
                        if (audioFormat == WaveAudioFormat.Unknown
                            || audioFormat == WaveAudioFormat.ALAW
                            || audioFormat == WaveAudioFormat.MULAW
                        ) throw new NotSupportedException($"WAVE format `{audioFormat}` not supported.");

                        // NumChannels:
                        numChannels = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(offset + 10)..(offset + 12)]);
                        if (numChannels == 0) throw new FormatException("Invalid FMT_ subchunk NumChannels is zero.");

                        // SampleRate:
                        sampleRate = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 12)..(offset + 16)]);

                        // ByteRate:
                        uint byteRate = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 16)..(offset + 20)]);

                        // BlockAlign:
                        blockAlign = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(offset + 20)..(offset + 22)]);

                        // BitsPerSample:
                        bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(offset + 22)..(offset + 24)]);

                        // Verify ByteRate:
                        int expectedByteRate = sampleRate * blockAlign;
                        if (byteRate != expectedByteRate) throw new FormatException("Unexpected FMT_ subchunk ByteRate value.");

                        /*
                         * This is the end of the FMT_ subchunk. Some applications may place additional data at the end of this
                         * sub-chunk.
                         */
                        break;
                    }
                    case "fact": { // fact sub-chunk
                        /*
                         * ------------------------------------------------- FACT SUBCHUNK -------------------------------------------------
                         * All (compressed) non-PCM formats must have a FACT chunk. The chunk contains at least one value: the number of
                         * samples in the file.
                         * 0    4   Subchunk3ID     Contains the letters "fact" in big endian ASCII format
                         * 4    4   Subchunk3Size   Number of bytes of data in this chunk.
                         * 8    4   NumSamples      Number of samples (per channel)
                         * 12   *                   Misc data.
                         */
                        containsFactSubchunk = true;

                        // NumSamples:
                        numSamples = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 8)..(offset + 12)]);

                        // TODO: check for additional data here
                        break;
                    }
                    case "data": { // data sub-chunk
                        /*
                         * ------------------------------------------------- DATA SUBCHUNK -------------------------------------------------
                         * This sub-chunk contains the size of the data and the actual audio:
                         * 0    4   Subchunk2ID     Contains the letters "data" in big endian ASCII format
                         * 4    4   Subchunk2Size   == NumSamples * NumChannels * BitsPerSample/8
                         *                          This is the number of bytes in the data. You can also think of this as the size of the
                         *                          read of the subchunk following this number.
                         * 8    *   Data            The actual sound data.
                         */

                        if (!containsFormatSubchunk) throw new FormatException("WAVE data did not contain a FMT_ sub-chunk before the DATA sub-chunk.");
                        if (!containsFactSubchunk) { // there is was no fact sub-chunk
                            /*
                             * Here we can calculate the number of samples from the Subchunk2Size:
                             * Since the Subchunk2Size is equal to `NumSamples * NumChannels * BitsPerSample/8` we can reverse engineer this to
                             * find NumSamples:
                             * 
                             * Subchunk2Size / (NumChannels * BitsPerSample/8) == NumSamples
                             */
                            numSamples = (int)(subchunkSize / blockAlign);
                        }

                        offset += 8; // move the offset to the start of the wave data

                        /*
                         * ----------------------------------------------- AudioClip Samples -----------------------------------------------
                         * The next Subchunk2Size number of bytes are all audio data. This data is formatted slightly differently depending
                         * on the number of bits/bytes per sample:
                         * BITS/SAMPLE      DESCRIPTION
                         * 8                Stored as unsigned BYTEs ranging from 0 to 255.
                         * 8+               Stored as signed BYTES (e.g. 16 bits is between -32768 and 32767).
                         */
                        int sampleCount = numSamples * numChannels;
                        float[] samples = new float[sampleCount];
                        switch (audioFormat) {
                            case WaveAudioFormat.PCM: {
                                switch (bitsPerSample) {
                                    case 8: { // ubyte
                                        for (int i = 0; i < sampleCount; i++) { // iterate each sample
                                            samples[i] = (bytes[offset + i] - 128) * _8BitPerSampleCoefficient;
                                        }
                                        break;
                                    }
                                    case 16: { // signed short
                                        int sampleIndex;
                                        for (int i = 0; i < sampleCount; i++) {
                                            sampleIndex = offset + (i * 2);
                                            samples[i] = BinaryPrimitives.ReadInt16LittleEndian(bytes[sampleIndex..(sampleIndex + 2)]) * _16BitPerSampleCoefficient;
                                        }
                                        break;
                                    }
                                    case 24: { // 3 signed bytes
                                        int sampleIndex;
                                        for (int i = 0; i < sampleCount; i++) {
                                            sampleIndex = offset + (i * 3);
                                            samples[i] = ((bytes[sampleIndex + 2] << 24 | bytes[sampleIndex + 1] << 16 | bytes[sampleIndex] << 8) >> 8) * _24BitPerSampleCoefficient;
                                        }
                                        break;
                                    }
                                    case 32: { // signed int
                                        int sampleIndex;
                                        for (int i = 0; i < sampleCount; i++) {
                                            sampleIndex = offset + (i * 4);
                                            samples[i] = BinaryPrimitives.ReadInt32LittleEndian(bytes[sampleIndex..(sampleIndex + 4)]) * _32BitPerSampleCoefficient;
                                        }
                                        break;
                                    }
                                    default: throw new NotSupportedException($"{bitsPerSample} bits per sample is not supported for WAVE format imports.");
                                }
                                break;
                            }
                            case WaveAudioFormat.IEEE_FLOAT: {
                                switch (bitsPerSample) {
                                    case 32: { // 32bit signed floating point number (float)
                                        if (BitConverter.IsLittleEndian) {
                                            for (int i = 0; i < sampleCount; i++) {
                                                samples[i] = BitConverter.ToSingle(bytes, offset + (i * 4));
                                            }
                                        } else {
                                            int sampleIndex;
                                            for (int i = 0; i < sampleCount; i++) {
                                                sampleIndex = offset + (i * 4);
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
                                                samples[i] = (float)BitConverter.ToDouble(bytes, offset + (i * 4));
                                            }
                                        } else {
                                            int sampleIndex;
                                            for (int i = 0; i < sampleCount; i++) {
                                                sampleIndex = offset + (i * 8);
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
                            default: throw new NotSupportedException($"WAVE format `{audioFormat}` not supported.");
                        }

                        // create audio clip:
                        AudioClip audioClip = AudioClip.Create(name, numSamples, numChannels, sampleRate, false);

                        // set audio clip data:
                        audioClip.SetData(samples, 0);

                        // return audio clip:
                        Debug.Log($"success: {audioFormat} {name} {fsr.FileName}");
                        return audioClip;
                    }
                }

                // move offset to expected start of the next sub-chunk:
                offset += (int)(subchunkSize + 8);
            }
            throw new FormatException($"WAVE data did not contain a DATA sub-chunk.");
        }

        #endregion

    }

}