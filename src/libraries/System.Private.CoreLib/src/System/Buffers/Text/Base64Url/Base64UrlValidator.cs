﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    public static partial class Base64Url
    {
        /// <summary>Validates that the specified span of text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64UrlText">A span of text to validate.</param>
        /// <returns><see langword="true"/> if <paramref name="base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="Base64Url.DecodeFromChars(ReadOnlySpan{char})"/> and
        /// <see cref="Base64Url.TryDecodeFromChars(ReadOnlySpan{char}, Span{byte}, out int)"/> would successfully decode (in the case
        /// of <see cref="Base64Url.TryDecodeFromChars(ReadOnlySpan{char}, Span{byte}, out int)"/> assuming sufficient output space).
        /// Any amount of whitespace is allowed anywhere in the input, where whitespace is defined as the characters ' ', '\t', '\r', or '\n'.
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<char> base64UrlText) =>
            Base64Helper.IsValid(default(Base64UrlCharValidatable), base64UrlText, out _);

        /// <summary>Validates that the specified span of text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64UrlText">A span of text to validate.</param>
        /// <param name="decodedLength">If the method returns true, the number of decoded bytes that will result from decoding the input text.</param>
        /// <returns><see langword="true"/> if <paramref name="base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="Base64Url.DecodeFromChars(ReadOnlySpan{char})"/> and
        /// <see cref="Base64Url.TryDecodeFromChars(ReadOnlySpan{char}, Span{byte}, out int)"/> would successfully decode (in the case
        /// of <see cref="Base64Url.TryDecodeFromChars(ReadOnlySpan{char}, Span{byte}, out int)"/> assuming sufficient output space).
        /// Any amount of whitespace is allowed anywhere in the input, where whitespace is defined as the characters ' ', '\t', '\r', or '\n'.
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<char> base64UrlText, out int decodedLength) =>
            Base64Helper.IsValid(default(Base64UrlCharValidatable), base64UrlText, out decodedLength);

        /// <summary>Validates that the specified span of UTF-8 text is comprised of valid base-64 encoded data.</summary>
        /// <param name="utf8Base64UrlText">A span of UTF-8 text to validate.</param>
        /// <returns><see langword="true"/> if <paramref name="utf8Base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n' (as bytes).
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<byte> utf8Base64UrlText) =>
            Base64Helper.IsValid(default(Base64UrlByteValidatable), utf8Base64UrlText, out _);

        /// <summary>Validates that the specified span of UTF-8 text is comprised of valid base-64 encoded data.</summary>
        /// <param name="utf8Base64UrlText">A span of UTF-8 text to validate.</param>
        /// <param name="decodedLength">If the method returns true, the number of decoded bytes that will result from decoding the input UTF-8 text.</param>
        /// <returns><see langword="true"/> if <paramref name="utf8Base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n' (as bytes).
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<byte> utf8Base64UrlText, out int decodedLength) =>
            Base64Helper.IsValid(default(Base64UrlByteValidatable), utf8Base64UrlText, out decodedLength);

        private const uint UrlEncodingPad = '%'; // allowed for url padding

        private readonly struct Base64UrlCharValidatable : Base64Helper.IBase64Validatable<char>
        {
#if NET
            private static readonly SearchValues<char> s_validBase64UrlChars = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_");

            public int IndexOfAnyExcept(ReadOnlySpan<char> span) => span.IndexOfAnyExcept(s_validBase64UrlChars);
#else
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int DecodeValue(char value)
            {
                if (value > byte.MaxValue)
                {
                    // Invalid char was found.
                    return -2;
                }

                return default(Base64UrlDecoderByte).DecodingMap[value];
            }
#endif
            public bool IsWhiteSpace(char value) => Base64Helper.IsWhiteSpace(value);
            public bool IsEncodingPad(char value) => value == Base64Helper.EncodingPad || value == UrlEncodingPad;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ValidateAndDecodeLength(char lastChar, int length, int paddingCount, out int decodedLength) =>
                default(Base64UrlByteValidatable).ValidateAndDecodeLength((byte)lastChar, length, paddingCount, out decodedLength);
        }

        private readonly struct Base64UrlByteValidatable : Base64Helper.IBase64Validatable<byte>
        {
#if NET
            private static readonly SearchValues<byte> s_validBase64UrlChars = SearchValues.Create(default(Base64UrlEncoderByte).EncodingMap);

            public int IndexOfAnyExcept(ReadOnlySpan<byte> span) => span.IndexOfAnyExcept(s_validBase64UrlChars);
#else
            public int DecodeValue(byte value) => default(Base64UrlDecoderByte).DecodingMap[value];
#endif
            public bool IsWhiteSpace(byte value) => Base64Helper.IsWhiteSpace(value);
            public bool IsEncodingPad(byte value) => value == Base64Helper.EncodingPad || value == UrlEncodingPad;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ValidateAndDecodeLength(byte lastChar, int length, int paddingCount, out int decodedLength)
            {
                // Padding is optional for Base64Url, so need to account remainder.
                int remainder = (int)((uint)length % 4);

                if (paddingCount != 0)
                {
                    length -= paddingCount;
                    remainder = (int)((uint)length % 4);

                    // if there is a padding, there should be remainder and the sum of remainder and padding should not exceed 4
                    if (remainder == 0 || remainder + paddingCount > 4)
                    {
                        decodedLength = 0;
                        return false;
                    }
                }

                decodedLength = (length >> 2) * 3 + (remainder > 0 ? remainder - 1 : 0);

                if (remainder > 0)
                {
                    int decoded = default(Base64UrlDecoderByte).DecodingMap[lastChar];
                    switch (remainder)
                    {
                        case 1: return false; // 1 byte is not decodable => invalid.
                        case 2: return ((decoded & 0x0F) == 0); // if unused lower 4 bits are set to 0
                        case 3: return ((decoded & 0x03) == 0); // if unused lower 2 bits are set to 0
                    }
                }

                return true;
            }
        }
    }
}
