using System;
using System.Collections;
using System.Threading.Tasks;

using StegoService.Core.Blocks;

namespace StegoService.Core.Helpers
{
    public static class DctHelpers
    {
        public static double FourierCoefficient(int x)
        {
            return x > 0 ? 1.0 : (1.0 / Math.Sqrt(2));
        }
    }

    public static class BitArrayHelpers
    {
        public static byte[] ToBytes(this BitArray bitArray)
        {
            int length = (bitArray.Count - 1) / 8 + 1;
            var byteArray = new byte[length];

            bitArray.CopyTo(byteArray, 0);

            return byteArray;
        }
    }

    public static class Precision
    {
        public const double OneE1   = 0.1;
        public const double OneE2   = 0.01;
        public const double OneE3   = 0.001;
        public const double OneE4   = 0.0001;
        public const double OneE5   = 0.00001;
        public const double OneE6   = 0.000001;
        public const double OneE7   = 0.0000001;
        public const double OneE8   = 0.00000001;
        public const double OneE9   = 0.000000001;
        public const double OneE10  = 0.0000000001;

        public static bool EqualsWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return Math.Abs(num1 - num2) < precision;
        }

        public static bool NotEqualsWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return Math.Abs(num1 - num2) >= precision;
        }

        public static bool LessThanWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return (num2 - num1) >= precision;
        }

        public static bool LessThanOrEqualWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return (num2 - num1) > precision || Math.Abs(num1 - num2) < precision;
        }

        public static bool GreaterThanWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return (num1 - num2) >= precision;
        }

        public static bool GreaterThanOrEqualWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return (num1 - num2) > precision || Math.Abs(num1 - num2) < precision;
        }
    }

    public static class BlocksHelpers
    {
        public static ByteBlock[] ToBlocksParallel(byte[,] matrix)
        {
            int x = (matrix.GetLength(1) / ByteBlock.Size) * ByteBlock.Size;
            int y = (matrix.GetLength(0) / ByteBlock.Size) * ByteBlock.Size;

            int blocksCount = (x * y) / (ByteBlock.Size * ByteBlock.Size);

            var blocks = new ByteBlock[blocksCount];

            Parallel.For(0, blocksCount, (index) =>
            {
                int x1 = (ByteBlock.Size * index % x);
                int y1 = (ByteBlock.Size * index / x) * ByteBlock.Size;

                blocks[index] = new ByteBlock(matrix, x1, y1);
            });

            return blocks;
        }

        public static byte[,] ToMatrixParallel(ByteBlock[] blocks, int width, int height)
        {
            var matrix = new byte[height, width];

            int x = (width / ByteBlock.Size) * ByteBlock.Size;
            int y = (height / ByteBlock.Size) * ByteBlock.Size;

            int blocksCount = (x * y) / (ByteBlock.Size * ByteBlock.Size);

            Parallel.For(0, blocksCount, (index) =>
            {
                int x1 = (ByteBlock.Size * index % x);
                int y1 = (ByteBlock.Size * index / x) * ByteBlock.Size;

                for (int i = 0; i < ByteBlock.Size; i++)
                {
                    for (int j = 0; j < ByteBlock.Size; j++)
                    {
                        matrix[i + y1, j + x1] = blocks[index][i, j];
                    }
                }
            });

            return matrix;
        }

        public static void FillMatrixParallel(byte[,] matrix, ByteBlock[] blocks)
        {
            int x = (matrix.GetLength(1) / ByteBlock.Size) * ByteBlock.Size;
            int y = (matrix.GetLength(0) / ByteBlock.Size) * ByteBlock.Size;

            int blocksCount = (x * y) / (ByteBlock.Size * ByteBlock.Size);

            Parallel.For(0, blocksCount, (index) =>
            {
                int x1 = (ByteBlock.Size * index % x);
                int y1 = (ByteBlock.Size * index / x) * ByteBlock.Size;

                for (int i = 0; i < ByteBlock.Size; i++)
                {
                    for (int j = 0; j < ByteBlock.Size; j++)
                    {
                        matrix[i + y1, j + x1] = blocks[index][i, j];
                    }
                }
            });
        }

        public static DctBlock[] TransformParallel(ByteBlock[] blocks)
        {
            int blockCount = blocks.Length;

            var transformedBlocks = new DctBlock[blockCount];

            Parallel.For(0, blockCount, (index) =>
            {
                transformedBlocks[index] = blocks[index].MakeDct();
            });

            return transformedBlocks;
        }

        public static ByteBlock[] ReverseTransformParallel(DctBlock[] transformedBlocks)
        {
            int blockCount = transformedBlocks.Length;

            var blocks = new ByteBlock[blockCount];

            Parallel.For(0, blockCount, (index) =>
            {
                blocks[index] = transformedBlocks[index].MakeInverseDct();
            });

            return blocks;
        }
    }
}
