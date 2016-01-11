﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL) license. See LICENSE file in the project root for full license information.

namespace PCLCrypto
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    /// <summary>
    /// An assortment of crypto utilities.
    /// </summary>
    internal static class CryptoUtilities
    {
        /// <summary>
        /// Grows a buffer as necessary to align with a block size.
        /// </summary>
        /// <param name="buffer">The buffer to grow.</param>
        /// <param name="blockLength">The length (in bytes) of a block.</param>
        internal static void ApplyZeroPadding(ref byte[] buffer, int blockLength)
        {
            Requires.NotNull(buffer, nameof(buffer));
            Requires.Range(blockLength > 0, nameof(blockLength));

            int bytesBeyondLastBlockLength = buffer.Length % blockLength;
            if (bytesBeyondLastBlockLength > 0)
            {
                int growBy = blockLength - bytesBeyondLastBlockLength;
                Array.Resize(ref buffer, buffer.Length + growBy);
            }
        }

        /// <summary>
        /// Grows a buffer as necessary to align with a block size.
        /// </summary>
        /// <param name="buffer">The buffer to grow.</param>
        /// <param name="blockLength">The length (in bytes) of a block.</param>
        /// <param name="bufferOffset">The index of the first byte in <paramref name="buffer"/> that is part of the message.</param>
        /// <param name="bufferCount">The number of bytes in <paramref name="buffer"/> that are part of the message.</param>
        internal static void ApplyZeroPadding(ref byte[] buffer, int blockLength, ref int bufferOffset, ref int bufferCount)
        {
            Requires.NotNull(buffer, nameof(buffer));

            int bytesBeyondLastBlockLength = bufferCount % blockLength;
            if (bytesBeyondLastBlockLength > 0)
            {
                int growBy = blockLength - bytesBeyondLastBlockLength;
                byte[] newBuffer = new byte[bufferCount + growBy];
                Array.Copy(buffer, bufferOffset, newBuffer, 0, bufferCount);
                buffer = newBuffer;
                bufferOffset = 0;
                bufferCount += growBy;
            }
        }

        /// <summary>
        /// Performs a constant time comparison between two buffers.
        /// </summary>
        /// <param name="buffer1">The first buffer.</param>
        /// <param name="buffer2">The second buffer.</param>
        /// <returns><c>true</c> if the buffers have exactly the same contents; <c>false</c> otherwise.</returns>
        internal static bool BufferEquals(byte[] buffer1, byte[] buffer2)
        {
            Requires.NotNull(buffer1, "buffer1");
            Requires.NotNull(buffer2, "buffer2");

            if (buffer1.Length != buffer2.Length)
            {
                return false;
            }

            // SECURITY NOTE: Do *not* fast path out of the loop once a mismatch is found.
            // That opens the door to timing security exploits. We must do all the work
            // no matter where a deviation may be.
            bool mismatchFound = false;
            for (int i = 0; i < buffer1.Length; i++)
            {
                mismatchFound |= buffer1[i] != buffer2[i];
            }

            return !mismatchFound;
        }

        /// <summary>
        /// Creates a copy of a byte array.
        /// </summary>
        /// <param name="buffer">The array to be copied. May be null.</param>
        /// <returns>The copy of the array, or null if <paramref name="buffer"/> was null.</returns>
        internal static byte[] CloneArray(this byte[] buffer)
        {
            if (buffer == null)
            {
                return null;
            }

            var result = new byte[buffer.Length];
            Array.Copy(buffer, result, buffer.Length);
            return result;
        }

        /// <summary>
        /// Gets an <see cref="ArraySegment{T}"/> for a given array, which may be null.
        /// </summary>
        /// <typeparam name="T">The type of element in the array.</typeparam>
        /// <param name="buffer">The array, which may be null.</param>
        /// <returns>The array segment.</returns>
        internal static ArraySegment<T> AsArraySegment<T>(this T[] buffer)
        {
            return buffer != null ? new ArraySegment<T>(buffer) : default(ArraySegment<T>);
        }

        /// <summary>
        /// Disposes a value if it is not null.
        /// </summary>
        /// <param name="value">The value to be disposed of.</param>
        internal static void DisposeIfNotNull(this IDisposable value)
        {
            if (value != null)
            {
                value.Dispose();
            }
        }

        /// <summary>
        /// Gets legal crypto key sizes for asymmetric algorithms (for platforms that do not expose the actual values).
        /// </summary>
        /// <param name="algorithm">The asymmetric algorithm whose keys are of interest to the caller.</param>
        /// <returns>A list of legal key size ranges.</returns>
        internal static IReadOnlyList<KeySizes> GetTypicalLegalAsymmetricKeySizes(this AsymmetricAlgorithm algorithm)
        {
            KeySizes range;
            switch (algorithm)
            {
                case AsymmetricAlgorithm.DsaSha1:
                case AsymmetricAlgorithm.DsaSha256:
                    range = new KeySizes(512, 1024, 64);
                    break;
                case AsymmetricAlgorithm.EcdsaP256Sha256:
                    range = new KeySizes(256, 256, 0);
                    break;
                case AsymmetricAlgorithm.EcdsaP384Sha384:
                    range = new KeySizes(384, 384, 0);
                    break;
                case AsymmetricAlgorithm.EcdsaP521Sha512:
                    range = new KeySizes(521, 521, 0);
                    break;
                case AsymmetricAlgorithm.RsaOaepSha1:
                case AsymmetricAlgorithm.RsaOaepSha256:
                case AsymmetricAlgorithm.RsaOaepSha384:
                case AsymmetricAlgorithm.RsaOaepSha512:
                case AsymmetricAlgorithm.RsaPkcs1:
                case AsymmetricAlgorithm.RsaSignPkcs1Sha1:
                case AsymmetricAlgorithm.RsaSignPkcs1Sha256:
                case AsymmetricAlgorithm.RsaSignPkcs1Sha384:
                case AsymmetricAlgorithm.RsaSignPkcs1Sha512:
                case AsymmetricAlgorithm.RsaSignPssSha1:
                case AsymmetricAlgorithm.RsaSignPssSha256:
                case AsymmetricAlgorithm.RsaSignPssSha384:
                case AsymmetricAlgorithm.RsaSignPssSha512:
                    range = new KeySizes(384, 16384, 8);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return new[] { range };
        }

        /// <summary>
        /// Allocates an array of characters to represent the specified string, with a null terminating character as the last array element.
        /// </summary>
        /// <param name="value">The string to represent as a character array.</param>
        /// <returns>The character array with null terminator.</returns>
        internal static char[] ToCharArrayWithNullTerminator(this string value)
        {
            Requires.NotNull(value, nameof(value));

            char[] buffer = new char[value.Length + 1];
            value.CopyTo(0, buffer, 0, value.Length);
            return buffer;
        }
    }
}
