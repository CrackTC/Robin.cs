using System.Text;

namespace Robin.Extensions.Oa.Fetcher;

internal static class OaDes
{
    static readonly int[] a = Str2Bits("1");
    static readonly int[] b = Str2Bits("2");
    static readonly int[] c = Str2Bits("3");

    public static string StrEnc(string data)
    {
        var encDataBuilder = new StringBuilder();
        var partBuilder = new StringBuilder();

        for (var i = 0; i < data.Length; i += 4)
        {
            var tmp = Str2Bits(data.Substring(i, Math.Min(4, data.Length - i)));
            tmp = Enc(tmp, a);
            tmp = Enc(tmp, b);
            tmp = Enc(tmp, c);

            partBuilder.Clear();
            partBuilder.Append('0', (64 - tmp.Length) / 4);

            for (var j = 0; j < tmp.Length; j += 4)
            {
                int num = 0;
                for (var k = 0; k < 4; k++) num = (num << 1) | tmp[j + k];
                partBuilder.Append(num.ToString("x"));
            }
            encDataBuilder.Append(partBuilder);
        }

        return encDataBuilder.ToString();
    }

    private static int[] Str2Bits(string str)
    {
        var paddedStr = str.PadRight(4, '\0')[..4];
        return paddedStr.SelectMany(c => Convert.ToString(c, 2).PadLeft(16, '0').Select(bit => int.Parse(bit.ToString()))).ToArray();
    }

    private static int[] Enc(int[] data, int[] key)
    {
        data = Init(data);
        var keys = GetKeys(key);

        var l = data[..32];
        var r = data[32..];

        for (var i = 0; i < 16; i++)
        {
            var tmp = l;
            l = r;
            r = Xor(tmp, P(S(Xor(keys[i], Expand(r)))));
        }

        return Final([.. r, .. l]);
    }

    private static readonly int[] init = [
        57, 49, 41, 33, 25, 17, 9, 1,
        59, 51, 43, 35, 27, 19, 11, 3,
        61, 53, 45, 37, 29, 21, 13, 5,
        63, 55, 47, 39, 31, 23, 15, 7,
        56, 48, 40, 32, 24, 16, 8, 0,
        58, 50, 42, 34, 26, 18, 10, 2,
        60, 52, 44, 36, 28, 20, 12, 4,
        62, 54, 46, 38, 30, 22, 14, 6,
    ];
    private static int[] Init(int[] data) => init.Select(index => data[index]).ToArray();

    private static readonly int[] expand = [
        31, 0, 1, 2, 3, 4, 3, 4,
        5, 6, 7, 8, 7, 8, 9, 10,
        11, 12, 11, 12, 13, 14, 15, 16,
        15, 16, 17, 18, 19, 20, 19, 20,
        21, 22, 23, 24, 23, 24, 25, 26,
        27, 28, 27, 28, 29, 30, 31, 0,
    ];
    private static int[] Expand(int[] data) => expand.Select(index => data[index]).ToArray();

    private static int[] Xor(int[] a, int[] b) => a.Zip(b, (x, y) => x ^ y).ToArray();

    private static readonly int[,,] s = new[, ,]
    {
        {
            { 14, 4, 13, 1, 2, 15, 11, 8, 3, 10, 6, 12, 5, 9, 0, 7 },
            { 0, 15, 7, 4, 14, 2, 13, 1, 10, 6, 12, 11, 9, 5, 3, 8 },
            { 4, 1, 14, 8, 13, 6, 2, 11, 15, 12, 9, 7, 3, 10, 5, 0 },
            { 15, 12, 8, 2, 4, 9, 1, 7, 5, 11, 3, 14, 10, 0, 6, 13 }
        },
        {
            { 15, 1, 8, 14, 6, 11, 3, 4, 9, 7, 2, 13, 12, 0, 5, 10 },
            { 3, 13, 4, 7, 15, 2, 8, 14, 12, 0, 1, 10, 6, 9, 11, 5 },
            { 0, 14, 7, 11, 10, 4, 13, 1, 5, 8, 12, 6, 9, 3, 2, 15 },
            { 13, 8, 10, 1, 3, 15, 4, 2, 11, 6, 7, 12, 0, 5, 14, 9 }
        },
        {
            { 10, 0, 9, 14, 6, 3, 15, 5, 1, 13, 12, 7, 11, 4, 2, 8 },
            { 13, 7, 0, 9, 3, 4, 6, 10, 2, 8, 5, 14, 12, 11, 15, 1 },
            { 13, 6, 4, 9, 8, 15, 3, 0, 11, 1, 2, 12, 5, 10, 14, 7 },
            { 1, 10, 13, 0, 6, 9, 8, 7, 4, 15, 14, 3, 11, 5, 2, 12 }
        },
        {
            { 7, 13, 14, 3, 0, 6, 9, 10, 1, 2, 8, 5, 11, 12, 4, 15 },
            { 13, 8, 11, 5, 6, 15, 0, 3, 4, 7, 2, 12, 1, 10, 14, 9 },
            { 10, 6, 9, 0, 12, 11, 7, 13, 15, 1, 3, 14, 5, 2, 8, 4 },
            { 3, 15, 0, 6, 10, 1, 13, 8, 9, 4, 5, 11, 12, 7, 2, 14 }
        },
        {
            { 2, 12, 4, 1, 7, 10, 11, 6, 8, 5, 3, 15, 13, 0, 14, 9 },
            { 14, 11, 2, 12, 4, 7, 13, 1, 5, 0, 15, 10, 3, 9, 8, 6 },
            { 4, 2, 1, 11, 10, 13, 7, 8, 15, 9, 12, 5, 6, 3, 0, 14 },
            { 11, 8, 12, 7, 1, 14, 2, 13, 6, 15, 0, 9, 10, 4, 5, 3 }
        },
        {
            { 12, 1, 10, 15, 9, 2, 6, 8, 0, 13, 3, 4, 14, 7, 5, 11 },
            { 10, 15, 4, 2, 7, 12, 9, 5, 6, 1, 13, 14, 0, 11, 3, 8 },
            { 9, 14, 15, 5, 2, 8, 12, 3, 7, 0, 4, 10, 1, 13, 11, 6 },
            { 4, 3, 2, 12, 9, 5, 15, 10, 11, 14, 1, 7, 6, 0, 8, 13 }
        },
        {
            { 4, 11, 2, 14, 15, 0, 8, 13, 3, 12, 9, 7, 5, 10, 6, 1 },
            { 13, 0, 11, 7, 4, 9, 1, 10, 14, 3, 5, 12, 2, 15, 8, 6 },
            { 1, 4, 11, 13, 12, 3, 7, 14, 10, 15, 6, 8, 0, 5, 9, 2 },
            { 6, 11, 13, 8, 1, 4, 10, 7, 9, 5, 0, 15, 14, 2, 3, 12 }
        },
        {
            { 13, 2, 8, 4, 6, 15, 11, 1, 10, 9, 3, 14, 5, 0, 12, 7 },
            { 1, 15, 13, 8, 10, 3, 7, 4, 12, 5, 6, 11, 0, 14, 9, 2 },
            { 7, 11, 4, 1, 9, 12, 14, 2, 0, 6, 10, 13, 15, 3, 5, 8 },
            { 2, 1, 14, 7, 4, 10, 8, 13, 15, 12, 9, 0, 3, 5, 6, 11 }
        }
    };

    private static int[] S(int[] bytes)
    {
        var res = new List<int>(32);

        for (var x = 0; x < 48; x += 6)
        {
            var i = (bytes[x] << 1) | bytes[x + 5];
            var j = (bytes[x + 1] << 3) | (bytes[x + 2] << 2) | (bytes[x + 3] << 1) | bytes[x + 4];

            var num = s[x / 6, i, j];

            for (var k = 0; k < 4; k++)
            {
                res.Add((num >> (3 - k)) & 1);
            }
        }

        return [.. res];
    }

    private static readonly int[] p = [
        15, 6, 19, 20, 28, 11, 27, 16,
        0, 14, 22, 25, 4, 17, 30, 9,
        1, 7, 23, 13, 31, 26, 2, 8,
        18, 12, 29, 5, 21, 10, 3, 24,
    ];
    private static int[] P(int[] bytes) => p.Select(index => bytes[index]).ToArray();

    private static readonly int[] final = [
        39, 7, 47, 15, 55, 23, 63, 31,
        38, 6, 46, 14, 54, 22, 62, 30,
        37, 5, 45, 13, 53, 21, 61, 29,
        36, 4, 44, 12, 52, 20, 60, 28,
        35, 3, 43, 11, 51, 19, 59, 27,
        34, 2, 42, 10, 50, 18, 58, 26,
        33, 1, 41, 9, 49, 17, 57, 25,
        32, 0, 40, 8, 48, 16, 56, 24,
    ];
    private static int[] Final(int[] endByte) => final.Select(index => endByte[index]).ToArray();

    private static readonly int[] k1 = [
        56, 48, 40, 32, 24, 16, 8, 0,
        57, 49, 41, 33, 25, 17, 9, 1,
        58, 50, 42, 34, 26, 18, 10, 2,
        59, 51, 43, 35, 27, 19, 11, 3,
        60, 52, 44, 36, 28, 20, 12, 4,
        61, 53, 45, 37, 29, 21, 13, 5,
        62, 54, 46, 38, 30, 22, 14, 6,
    ];
    private static readonly int[] k2 = [
        13, 16, 10, 23, 0, 4,
        2, 27, 14, 5, 20, 9,
        22, 18, 11, 3, 25, 7,
        15, 6, 26, 19, 12, 1,
        40, 51, 30, 36, 46, 54,
        29, 39, 50, 44, 32, 47,
        43, 48, 38, 55, 33, 52,
        45, 41, 49, 35, 28, 31,
    ];
    private static readonly int[] loop = [1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1];
    private static int[][] GetKeys(int[] src)
    {
        var keys = new int[16][];

        var key = k1.Select(index => src[index]).ToArray();

        for (var i = 0; i < 16; i++)
        {
            for (var j = 0; j < loop[i]; j++)
            {
                var first = key[0];
                var second = key[28];
                Array.Copy(key, 1, key, 0, 27);
                key[27] = first;
                Array.Copy(key, 29, key, 28, 27);
                key[55] = second;
            }

            keys[i] = k2.Select(index => key[index]).ToArray();
        }

        return keys;
    }
}
