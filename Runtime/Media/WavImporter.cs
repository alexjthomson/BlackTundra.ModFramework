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
        private static readonly byte[] FMT__SubchunkID = Encoding.GetBytes("fmt ");
        private static readonly byte[] FACT_SubchunkID = Encoding.GetBytes("fact");
        private static readonly byte[] DATA_SubchunkID = Encoding.GetBytes("data");

        private const float _8BitPerSampleCoefficient = 1.0f / 128.0f;
        private const float _16BitPerSampleCoefficient = 1.0f / 32768.0f;
        private const float _24BitPerSampleCoefficient = 1.0f / 8388608.0f;
        private const float _32BitPerSampleCoefficient = 1.0f / 2147483648.0f;

        #endregion

        #region enum

        /// <summary>
        /// Describes the format of some WAVE data.
        /// </summary>
        private enum WaveFormat : ushort {
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
             * - Add dynamic sub-chunk ordering (allow the sub-chunks to appear in any order)
             * - Add support for ignoring unrecognized chunks (such as `JUNK`) and ignoring and moving onto the
             *   next chunk.
             * - Review & tidy up audio data processing switch statements. There is a chance I'm converting the
             *   integers into floats wrong since I didn't research how the conversion should be done.
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
            // The ChunkSize is verified later using the equation in the above ChunkSize definition

            // Format:
            if (!bytes.ContentEquals(8, RIFF_HeaderExpectedFileType, 0, 4)) throw new FormatException("Invalid RIFF header format field.");

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
            if (!bytes.ContentEquals(12, FMT__SubchunkID, 0, 4)) throw new FormatException("Invalid FMT_ subchunk ChunkID.");

            // Subchunk1Size:
            uint subchunk1Size = BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..20]);

            // AudioFormat:
            ushort audioFormatCode = BinaryPrimitives.ReadUInt16LittleEndian(bytes[20..22]);
            if (!((ushort[])Enum.GetValues(typeof(WaveFormat))).Contains(audioFormatCode)) {
                throw new NotSupportedException($"Unsupported FMT_ subchunk WAVE format `{audioFormatCode}`.");
            }
            WaveFormat audioFormat = (WaveFormat)audioFormatCode;
            if (audioFormat == WaveFormat.ALAW || audioFormat == WaveFormat.MULAW) throw new NotSupportedException($"WAVE format `{audioFormat}` not supported.");

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
            int chunkOffset = (int)(subchunk1Size - 16);

            // Create NumSamples (this is defined in the FACT sub-chunk but can otherwise be calculated later):
            int numSamples = -1;

            // FACT sub-chunk:
            if (audioFormat == WaveFormat.EXTENSIBLE) {
                /*
                 * ------------------------------------------------- FACT SUBCHUNK -------------------------------------------------
                 * All (compressed) non-PCM formats must have a FACT chunk. The chunk contains at least one value: the number of
                 * samples in the file.
                 * Let X = the additional offset from previous chunks:
                 * 36+X 4   Subchunk3ID     Contains the letters "fact" in big endian ASCII format
                 * 40+X 4   Subchunk3Size   Number of bytes of data in this chunk.
                 * 44+X 4   NumSamples      Number of samples (per channel)
                 * 48+X *                   Misc data.
                 */

                /*
                 * We should now check for the presence of the FACT sub-chunk. If the ChunkID is not present we should not throw a
                 * FormatException; instead, we should just move on and assume the FACT sub-chunk is not included in the file. In the
                 * case that the FACT sub-chunk is not included, the next expected chunk is the DATA chunk.
                 */
                if (bytes.ContentEquals(36 + chunkOffset, FACT_SubchunkID, 0, 4)) { // Subchunk3ID:
                    // Subchunk3Size:
                    int subchunk3Size = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[(44 + chunkOffset)..(48 + chunkOffset)]);

                    // NumSamples:
                    numSamples = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[(44 + chunkOffset)..(48 + chunkOffset)]);

                    /*
                     * Here we can calculate the amount of additional data in this subchunk that can be ignored and add it onto the
                     * chunk offset:
                     * 
                     * Since we know the expected size of the current chunk (4) and the actual size, we can find the number of extra
                     * bytes and add this to the chunk offset:
                     */
                    chunkOffset += subchunk3Size - 4;
                }
            }

            /*
             * ------------------------------------------------- DATA SUBCHUNK -------------------------------------------------
             * This sub-chunk contains the size of the data and the actual audio:
             * Let X = the additional offset from previous chunks:
             * 36+X 4   Subchunk2ID     Contains the letters "data" in big endian ASCII format
             * 40+X 4   Subchunk2Size   == NumSamples * NumChannels * BitsPerSample/8
             *                          This is the number of bytes in the data. You can also think of this as the size of the
             *                          read of the subchunk following this number.
             * 44+X *   Data            The actual sound data.
             */

            /*
             * Here we can calculate the start index for the samples of actual audio based off of the offset from the previous chunk(s):
             */
            int sampleStartIndex = 44 + chunkOffset;

            // Subchunk2ID:
            if (!bytes.ContentEquals(36 + chunkOffset, DATA_SubchunkID, 0, 4)) throw new FormatException("Invalid DATA subchunk ChunkID.");

            // Subchunk2Size:
            uint subchunk2Size = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(40 + chunkOffset)..sampleStartIndex]);

            /*
             * Here we can verify the ChunkSize. We cannot calculate the exact value for certain since there may be additional data or padding after
             * the audio data; however, this does mean we can at least calculate a minimum value:
             */
            uint minimumChunkSize = 4 + (8 + subchunk1Size) + (8 + subchunk2Size);
            //Debug.Log($"{fsr.FileName} {(chunkSize - minimumChunkSize)} {audioFormat} 1:{subchunk1Size} 2:{subchunk2Size} additional space: {chunkOffset}");
            if (chunkSize < minimumChunkSize) throw new FormatException($"Unexpected RIFF header ChunkSize (actual: `{chunkSize}`, expected_minimum: `{minimumChunkSize}`).");

            if (numSamples == -1) { // the number of samples still has not been calculated
                /*
                 * Here we can calculate the number of samples from the Subchunk2Size:
                 * Since the Subchunk2Size is equal to `NumSamples * NumChannels * BitsPerSample/8` we can reverse engineer this to
                 * find NumSamples:
                 * 
                 * Subchunk2Size / (NumChannels * BitsPerSample/8) == NumSamples
                 */
                numSamples = (int)(subchunk2Size / blockAlign);
            }

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
                case WaveFormat.PCM: {
                    switch (bitsPerSample) {
                        case 8: { // ubyte
                            for (int i = 0; i < sampleCount; i++) { // iterate each sample
                                samples[i] = (bytes[sampleStartIndex + i] - 128) * _8BitPerSampleCoefficient;
                            }
                            break;
                        }
                        case 16: { // signed short
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
                        case 32: { // signed int
                            int sampleIndex;
                            for (int i = 0; i < sampleCount; i++) {
                                sampleIndex = sampleStartIndex + (i * 4);
                                samples[i] = BinaryPrimitives.ReadInt32LittleEndian(bytes[sampleIndex..(sampleIndex + 4)]) * _32BitPerSampleCoefficient;
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
                default: throw new NotSupportedException($"WAVE format `{audioFormat}` not supported.");
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