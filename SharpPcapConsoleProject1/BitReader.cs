public class BitReader
{
    private BinaryReader reader;
    private byte currentByte;
    private int bitsLeft;

    public BitReader(BinaryReader reader)
    {
        this.reader = reader;
        bitsLeft = 0;
    }

    public int ReadBit()
    {
        if (bitsLeft == 0)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
                throw new EndOfStreamException();

            currentByte = reader.ReadByte();
            bitsLeft = 8;
        }

        bitsLeft--;
        return (currentByte >> bitsLeft) & 0x01;
    }

    public uint ReadBits(int n)
    {
        uint result = 0;
        for (int i = 0; i < n; i++)
        {
            result <<= 1;
            result |= (uint)ReadBit();
        }
        return result;
    }

    /// <summary>
    /// Reads an unsigned Exp-Golomb-coded integer from the bitstream.
    /// </summary>///////////////////////////////////////
    /// <param name="reader">BinaryReader to read from.</param>
    /// <returns>The decoded unsigned integer.</returns>

    public uint ReadUE()
    {
        int zeroBits = 0;
        while (ReadBit() == 0)
        {
            zeroBits++;
            if (zeroBits > 31)
                throw new Exception("Invalid Exp-Golomb code");
        }

        uint result = (1u << zeroBits) - 1u + ReadBits(zeroBits);
        return result;
    }

    /// <summary>
    /// Reads a signed Exp-Golomb-coded integer from the bitstream.
    /// </summary>
    /// <param name="reader">BinaryReader to read from.</param>
    /// <returns>The decoded signed integer.</returns>
    public int ReadSE()
    {
        uint codeNum = ReadUE();
        int value = (int)((codeNum % 2 == 0) ? -(codeNum / 2) : (codeNum + 1) / 2);
        return value;
    }

    public bool IsByteAligned
    {
        get { return bitsLeft % 8 == 0 || bitsLeft == 0; }
    }
}
