namespace MicroM.Core;

public class Base32
{
    public static string Base32Encode(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return string.Empty;

        int outputLength = (data.Length * 8 + 4) / 5;

        return string.Create(outputLength, data, (span, state) =>
        {
            ReadOnlySpan<char> base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

            int buffer = state[0];
            int bitsLeft = 8;
            int index = 1;
            int spanIndex = 0;

            while (bitsLeft > 0 || index < state.Length)
            {
                if (bitsLeft < 5)
                {
                    if (index < state.Length)
                    {
                        buffer = (buffer << 8) | state[index++];
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                int value = (buffer >> (bitsLeft - 5)) & 0x1F;
                bitsLeft -= 5;

                span[spanIndex++] = base32Chars[value];
            }
        });
    }

    public static byte[] Base32Decode(ReadOnlySpan<char> encoded)
    {
        encoded = encoded.TrimEnd('=');

        if (encoded.IsEmpty) return [];

        int outputLength = encoded.Length * 5 / 8;
        byte[] output = new byte[outputLength];

        int buffer = 0;
        int bitsLeft = 0;
        int index = 0;

        foreach (char c in encoded)
        {
            int value = char.ToUpperInvariant(c) switch
            {
                >= 'A' and <= 'Z' => c - 'A',
                >= '2' and <= '7' => c - '2' + 26,
                _ => throw new ArgumentException($"Invalid base32 character: {c}")
            };

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output[index++] = (byte)(buffer >> (bitsLeft - 8));
                bitsLeft -= 8;
            }
        }

        return output;
    }
}
