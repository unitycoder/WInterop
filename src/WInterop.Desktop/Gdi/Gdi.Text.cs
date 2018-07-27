﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using WInterop.Gdi.Native;
using WInterop.Support.Buffers;
using WInterop.Windows;

namespace WInterop.Gdi
{
    public static partial class Gdi
    {
        public static FontHandle GetStockFont(StockFont font) => new FontHandle((HFONT)Imports.GetStockObject((int)font), ownsHandle: false);

        public static Color GetTextColor(in DeviceContext context) => Imports.GetTextColor(context);

        public static Color SetTextColor(in DeviceContext context, Color color) => Imports.SetTextColor(context, color);

        public static TextAlignment SetTextAlignment(in DeviceContext context, TextAlignment alignment) => Imports.SetTextAlign(context, alignment);

        public static bool TextOut(in DeviceContext context, Point position, ReadOnlySpan<char> text)
            => Imports.TextOutW(context, position.X, position.Y, ref MemoryMarshal.GetReference(text), text.Length);

        public static unsafe int DrawText(in DeviceContext context, ReadOnlySpan<char> text, Rectangle bounds, TextFormat format)
        {
            RECT rect = bounds;

            if ((format & TextFormat.ModifyString) == 0)
            {
                // The string won't be changed, we can just pin
                return Imports.DrawTextW(context, ref MemoryMarshal.GetReference(text), text.Length, ref rect, format);
            }

            char[] buffer = ArrayPool<char>.Shared.Rent(text.Length + 5);
            try
            {
                Span<char> span = buffer.AsSpan();
                text.CopyTo(span);
                return Imports.DrawTextW(context, ref MemoryMarshal.GetReference(span), buffer.Length, ref rect, format);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        public static bool GetTextMetrics(in DeviceContext context, out TEXTMETRIC metrics) => Imports.GetTextMetricsW(context, out metrics);

        public static FontHandle CreateFont(
             int height,
             int width,
             int escapement,
             int orientation,
             FontWeight weight,
             bool italic,
             bool underline,
             bool strikeout,
             CharacterSet characterSet,
             OutputPrecision outputPrecision,
             ClippingPrecision clippingPrecision,
             Quality quality,
             FontPitch pitch,
             FontFamilyType family,
             string typeface)
        {
            return new FontHandle(Imports.CreateFontW(
                height, width, escapement, orientation,
                weight, italic, underline, strikeout,
                (uint)characterSet, (uint)outputPrecision, (uint)clippingPrecision, (uint)quality, (uint)((byte)pitch | (byte)family), typeface));
        }

        private static int EnumerateFontCallback(
            ref ENUMLOGFONTEXDV fontAttributes,
            ref NEWTEXTMETRICEX textMetrics,
            FontTypes fontType,
            LPARAM lParam)
        {
            var info = (List<FontInformation>)GCHandle.FromIntPtr(lParam).Target;
            info.Add(new FontInformation { FontType = fontType, TextMetrics = textMetrics, FontAttributes = fontAttributes });
            return 1;
        }

        public static IEnumerable<FontInformation> EnumerateFontFamilies(in DeviceContext context, CharacterSet characterSet, string faceName)
        {
            LOGFONT logFont = new LOGFONT
            {
                lfCharSet = characterSet,
            };

            logFont.lfFaceName.CopyFrom(faceName);

            List<FontInformation> info = new List<FontInformation>();
            GCHandle gch = GCHandle.Alloc(info, GCHandleType.Normal);
            try
            {
                int result = Imports.EnumFontFamiliesExW(context, ref logFont, EnumerateFontCallback, GCHandle.ToIntPtr(gch), 0);
            }
            finally
            {
                gch.Free();
            }

            return info;
        }
    }
}
