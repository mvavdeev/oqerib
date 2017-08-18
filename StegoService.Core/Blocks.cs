using System;

using StegoService.Core.Helpers;

namespace StegoService.Core.Blocks
{
    public class Block<T>
    {
        public static readonly int Size = 8;

        protected T[,] block = new T[Size, Size];

        public Block()
        {
        }

        public Block(Block<T> previousBlock)
        {
            for (int i = 0; i < Block<T>.Size; i++)
            {
                for (int j = 0; j < Block<T>.Size; j++)
                {
                    block[i, j] = previousBlock.block[i, j];
                }
            }
        }

        public Block(T[,] matrix, int x, int y)
        {
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    block[i, j] = matrix[i + y, j + x];
                }
            }
        }

        public T this[int y, int x]
        {
            get { return block[y, x]; }
            set { block[y, x] = value; }
        }
    }

    public sealed class ByteBlock : Block<byte>
    {
        public ByteBlock() : base()
        {
        }

        public ByteBlock(ByteBlock block) : base(block)
        {
        }

        public ByteBlock(byte[,] matrix, int x, int y) : base(matrix, x, y)
        {
        }

        public DctBlock MakeDct()
        {
            var transformed = new DctBlock();

            double sum;

            for (int v = 0; v < DctBlock.Size; v++)
            {
                for (int u = 0; u < DctBlock.Size; u++)
                {
                    sum = 0;

                    for (int i = 0; i < ByteBlock.Size; i++)
                    {
                        for (int j = 0; j < ByteBlock.Size; j++)
                        {
                            sum += block[i, j] *
                                Math.Cos((Math.PI * v * (2 * i + 1)) / (2 * ByteBlock.Size)) *
                                Math.Cos((Math.PI * u * (2 * j + 1)) / (2 * ByteBlock.Size));
                        }
                    }    
                                    
                    transformed[v, u] = (DctHelpers.FourierCoefficient(v) * DctHelpers.FourierCoefficient(u) * sum) / Math.Sqrt(2 * ByteBlock.Size);
                }
            }

            return transformed;
        }
    }

    public sealed class DctBlock : Block<double>
    {
        private const int point1X = 6, point1Y = 2;
        private const int point2X = 4, point2Y = 4;
        private const int point3X = 2, point3Y = 6;

        public const double MinHighFrequency = 40;
        public const double MaxLowFrequency = 2600;
        public const double Modifier = 50;

        public DctBlock() : base()
        {
        }

        public DctBlock(DctBlock block) : base(block)
        {
        }

        public DctBlock(double[,] matrix, int x, int y) : base(matrix, x, y)
        {
        }

        public double Point1
        {
            get { return block[point1Y, point1X]; }
            set { block[point1Y, point1X] = value; }
        }

        public double Point2
        {
            get { return block[point2Y, point2X]; }
            set { block[point2Y, point2X] = value; }
        }

        public double Point3
        {
            get { return block[point3Y, point3X]; }
            set { block[point3Y, point3X] = value; }
        }

        public ByteBlock MakeInverseDct()
        {
            var block = new ByteBlock();

            double sum;

            for (int v = 0; v < ByteBlock.Size; v++)
            {
                for (int u = 0; u < ByteBlock.Size; u++)
                {
                    sum = 0;

                    for (int i = 0; i < DctBlock.Size; i++)
                    {
                        for (int j = 0; j < DctBlock.Size; j++)
                        {
                            sum += (DctHelpers.FourierCoefficient(i) * DctHelpers.FourierCoefficient(j) * base.block[i, j]) *
                                Math.Cos((Math.PI * i * (2 * v + 1)) / (2 * DctBlock.Size)) *
                                Math.Cos((Math.PI * j * (2 * u + 1)) / (2 * DctBlock.Size));
                        }
                    }

                    sum = sum / Math.Sqrt(2.0 * DctBlock.Size);

                    if (sum > 255)
                    {
                        block[v, u] = 255;
                    }
                    else if (sum < 0)
                    {
                        block[v, u] = 0;
                    }
                    else
                    {
                        block[v, u] = Convert.ToByte(sum);
                    }
                }
            }

            return block;
        }

        public bool TestSmoothness()
        {
            double highFrequency = 0;

            int lastCell = DctBlock.Size;

            for (int i = 2; i < DctBlock.Size; i++)
            {
                for (int j = lastCell - 1; j < DctBlock.Size; j++)
                {
                    highFrequency += Math.Abs(block[i, j]);
                }
                lastCell--;
            }

            if (highFrequency.GreaterThanWithPrecision(MinHighFrequency, Precision.OneE3))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TestSharpness()
        {
            double lowFrequency = 0;

            int lastCell = DctBlock.Size - 1;

            for (int i = 0; i < DctBlock.Size - 1; i++)
            {
                for (int j = 0; j < lastCell; j++)
                {
                    lowFrequency += Math.Abs(block[i, j]);
                }
                lastCell--;
            }

            lowFrequency -= Math.Abs(block[0, 0]);

            if (lowFrequency.LessThanWithPrecision(MaxLowFrequency, Precision.OneE3))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TestEmbeddableness(bool bit)
        {
            var testBlock = new DctBlock(this);

            bool result;

            testBlock.EmbedBit(bit);
            testBlock = testBlock.MakeInverseDct().MakeDct();

            if (testBlock.TestSharpness() && testBlock.TestSmoothness() && testBlock.TryExtractBit(out result))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsSuitable()
        {
            return TestSharpness() && TestSmoothness();
        }

        public void EmbedBit(bool bit)
        {
            double point1 = block[point1Y, point1X];
            double point2 = block[point2Y, point2X];
            double point3 = block[point3Y, point3X];

            if (bit)
            {
                if (point3.LessThanOrEqualWithPrecision(Math.Max(point1, point2), Precision.OneE3))
                {
                    point3 = Math.Max(point1, point2) + Modifier / 2;

                    if (point1 > point2)
                    {
                        point1 -= Modifier / 2;
                    }
                    else
                    {
                        point2 -= Modifier / 2;
                    }
                }
            }
            else
            {
                if (point3.GreaterThanOrEqualWithPrecision(Math.Min(point1, point2), Precision.OneE3))
                {
                    point3 = Math.Min(point1, point2) - Modifier / 2;

                    if (point1 < point2)
                    {
                        point1 += Modifier / 2;
                    }
                    else
                    {
                        point2 += Modifier / 2;
                    }
                }
            }

            block[point1Y, point1X] = point1;
            block[point2Y, point2X] = point2;
            block[point3Y, point3X] = point3;
        }

        public bool ExtractBit()
        {
            bool result;

            if (TryExtractBit(out result))
            {
                return result;
            }
            else
            {
                throw new InvalidOperationException("Failed to extract bit.");
            }
        }

        public bool TryExtractBit(out bool bit)
        {
            double point1 = block[point1Y, point1X];
            double point2 = block[point2Y, point2X];
            double point3 = block[point3Y, point3X];

            if (point3.LessThanWithPrecision(Math.Min(point1, point2), Precision.OneE3))
            {
                bit = false;
                return true;
            }
            else if (point3.GreaterThanWithPrecision(Math.Max(point1, point2), Precision.OneE3))
            {
                bit = true;
                return true;
            }
            else
            {
                bit = false;
                return false;
            }
        }
    }
}
