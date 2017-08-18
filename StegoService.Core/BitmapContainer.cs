using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;

using StegoService.Core.Helpers;

using Medallion;

namespace StegoService.Core.BitmapContainer
{
    public enum ArgbChannels
    {
        Alpha,
        Red,
        Green,
        Blue
    }

    public sealed class BitmapContainer
    {
        private readonly Bitmap bitmap;

        private readonly int height;
        private readonly int width;

        private byte[,] alphaChannel, redChannel, greenChannel, blueChannel;

        public Bitmap Bitmap
        {
            get { return bitmap; }
        }

        public int Height
        {
            get { return height; }
        }

        public int Width
        {
            get { return width; }
        }

        public BitmapContainer(Bitmap bitmap)
        {
            this.bitmap = bitmap;

            height = this.bitmap.Height;
            width = this.bitmap.Width;

            alphaChannel = new byte[height, width];
            redChannel   = new byte[height, width];
            greenChannel = new byte[height, width];
            blueChannel  = new byte[height, width];

            SplitColors();
        }

        public void ResetColors()
        {
            SplitColors();
        }

        private void SplitColors()
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    var color = bitmap.GetPixel(j, i);

                    alphaChannel[i, j] = color.A;
                    redChannel[i, j]   = color.R;
                    greenChannel[i, j] = color.G;
                    blueChannel[i, j]  = color.B;
                }
            }
        }

        public void BuildFromColors()
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    var color = Color.FromArgb(alphaChannel[i, j], redChannel[i, j], greenChannel[i, j], blueChannel[i, j]);
                    bitmap.SetPixel(j, i, color);
                }
            }
        }

        public void EmbedStego(string text)
        {
            if (!TryEmbedString(text))
            {
                throw new InvalidOperationException("Not enough space.");
            }
        }

        public bool TryEmbedBits(byte[] bytes)
        {
            return TryEmbedBits(bytes, bytes.Length * 8);
        }

        public bool TryEmbedBits(byte[] bytes, int bitCount)
        {
            var bitArray = new BitArray(bytes);

            var blocks = BlocksHelpers.ToBlocksParallel(blueChannel);

            var transformedBlocks = BlocksHelpers.TransformParallel(blocks);

            var suitableBlocks = transformedBlocks
                .Where(block => block.IsSuitable())
                .ToArray();

            if (suitableBlocks.Length < bitCount)
            {
                return false;
            }

            int i = 0;

            foreach (var block in suitableBlocks)
            {
                if (i == bitCount)
                {
                    break;
                }
                else if (block.TestEmbeddableness(bitArray[i]))
                {
                    block.EmbedBit(bitArray[i]);
                    i++;
                }
                else
                {
                    block.EmbedBit(bitArray[i]);
                }
            }

            if (i != bitCount)
            {
                return false;
            }

            blocks = BlocksHelpers.ReverseTransformParallel(transformedBlocks);

            BlocksHelpers.FillMatrixParallel(blueChannel, blocks);
            BuildFromColors();
            return true;
        }

        public bool TryEmbedString(string text)
        {
            var strBytes = Encoding.UTF8.GetBytes(text + '\0');
            return TryEmbedBits(strBytes, strBytes.Length * 8);
        }
        
        public string ExtractStego()
        {
            var blocks = BlocksHelpers.ToBlocksParallel(blueChannel);

            var transformedBlocks = BlocksHelpers.TransformParallel(blocks);

            var suitableBlocks = transformedBlocks
                .Where(block => block.IsSuitable());            
            var byteList = new List<byte>();
            var bitArray = new BitArray(8);
            int bitCount = 0;
            foreach (var block in suitableBlocks)
            {
                bool bit;
                if (block.TryExtractBit(out bit))
                {
                    bitArray[bitCount] = bit;
                    bitCount++;
                    if (bitCount == 8)
                    {
                        var byteArray = bitArray.ToBytes();
                        if (byteArray[0] == 0)
                            break;
                        bitCount = 0;
                        byteList.Add(byteArray[0]);
                    }
                }
            }
            return Encoding.UTF8.GetString(byteList.ToArray());
        }

        public void AddNoise(ArgbChannels channel)
        {
            var random = new Random();

            byte[,] matrix = null;

            switch (channel)
            {
                case ArgbChannels.Alpha:
                    matrix = alphaChannel;
                    break;
                case ArgbChannels.Red:
                    matrix = redChannel;
                    break;
                case ArgbChannels.Green:
                    matrix = greenChannel;
                    break;
                case ArgbChannels.Blue:
                    matrix = blueChannel;
                    break;
            }

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    byte mod = (byte)(1 + random.NextGaussian());

                    if (matrix[i, j] + mod > 255)
                    {
                        matrix[i, j] -= mod;
                    }
                    else
                    {
                        matrix[i, j] += mod;
                    }                            
                }
            }
        }
    }
}
