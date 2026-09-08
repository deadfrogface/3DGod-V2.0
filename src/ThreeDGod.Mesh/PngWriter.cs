using System.Buffers.Binary;
using System.IO.Compression;
using System.Numerics;

namespace ThreeDGod.Mesh;

public static class PngWriter
{
    public static void WriteRgba(string path, int width, int height, byte[] rgba)
    {
        if (rgba.Length != width * height * 4)
            throw new InvalidOperationException("RGBA buffer size mismatch.");
        using var raw = new MemoryStream();
        for (var y = 0; y < height; y++)
        {
            raw.WriteByte(0);
            raw.Write(rgba, y * width * 4, width * 4);
        }
        var compressed = Compress(raw.ToArray());
        using var fs = File.Create(path);
        fs.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        WriteChunk(fs, "IHDR", Ihdr(width, height));
        WriteChunk(fs, "IDAT", compressed);
        WriteChunk(fs, "IEND", []);
    }

    public static void WriteMeshPreview(string path, IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices, int size = 128)
    {
        var pixels = new byte[size * size * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 30;
            pixels[i + 1] = 30;
            pixels[i + 2] = 36;
            pixels[i + 3] = 255;
        }
        if (positions.Count == 0)
        {
            WriteRgba(path, size, size, pixels);
            return;
        }
        var min = positions[0];
        var max = positions[0];
        foreach (var p in positions)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        var extent = MathF.Max(max.X - min.X, MathF.Max(max.Y - min.Y, 1e-4f));
        for (var t = 0; t + 2 < indices.Count; t += 3)
        {
            var a = Project(positions[indices[t]], min, extent, size);
            var b = Project(positions[indices[t + 1]], min, extent, size);
            var c = Project(positions[indices[t + 2]], min, extent, size);
            Fill(pixels, size, a, b, c);
        }
        WriteRgba(path, size, size, pixels);
    }

    private static Vector2 Project(Vector3 p, Vector3 min, float extent, int size)
    {
        var u = (p.X - min.X) / extent;
        var v = 1f - (p.Y - min.Y) / extent;
        return new Vector2(u * (size - 1), v * (size - 1));
    }

    private static void Fill(byte[] pixels, int size, Vector2 a, Vector2 b, Vector2 c)
    {
        var minX = (int)MathF.Max(0, MathF.Floor(MathF.Min(a.X, MathF.Min(b.X, c.X))));
        var maxX = (int)MathF.Min(size - 1, MathF.Ceiling(MathF.Max(a.X, MathF.Max(b.X, c.X))));
        var minY = (int)MathF.Max(0, MathF.Floor(MathF.Min(a.Y, MathF.Min(b.Y, c.Y))));
        var maxY = (int)MathF.Min(size - 1, MathF.Ceiling(MathF.Max(a.Y, MathF.Max(b.Y, c.Y))));
        var area = Edge(a, b, c);
        if (MathF.Abs(area) < 1e-4f)
            return;
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var w0 = Edge(b, c, p) / area;
                var w1 = Edge(c, a, p) / area;
                var w2 = Edge(a, b, p) / area;
                if (w0 < 0 || w1 < 0 || w2 < 0)
                    continue;
                var i = (y * size + x) * 4;
                pixels[i] = 196;
                pixels[i + 1] = 196;
                pixels[i + 2] = 210;
                pixels[i + 3] = 255;
            }
        }
    }

    private static float Edge(Vector2 a, Vector2 b, Vector2 c) =>
        (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);

    private static byte[] Ihdr(int width, int height)
    {
        var data = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(4, 4), height);
        data[8] = 8;
        data[9] = 6;
        return data;
    }

    private static byte[] Compress(byte[] raw)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Fastest, leaveOpen: true))
            zlib.Write(raw);
        return output.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var header = new byte[4 + 4 + data.Length];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), data.Length);
        header[4] = (byte)type[0];
        header[5] = (byte)type[1];
        header[6] = (byte)type[2];
        header[7] = (byte)type[3];
        data.CopyTo(header, 8);
        stream.Write(header);
        stream.Write(Crc32(header.AsSpan(4)));
    }

    private static byte[] Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xffffffff;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? 0xedb88320 ^ (crc >> 1) : crc >> 1;
        }
        crc ^= 0xffffffff;
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, crc);
        return bytes;
    }
}
