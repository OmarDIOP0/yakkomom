using System.Buffers.Binary;

namespace Yakkomom.Stockage;

public record InfosFichier(string TypeMime, string Extension, int? Largeur, int? Hauteur);

/// <summary>
/// Identifie le VRAI type d'un fichier à partir de ses premiers octets (signature),
/// sans faire confiance à l'extension ni au Content-Type envoyés par le navigateur,
/// et lit les dimensions des images directement dans leur en-tête.
/// </summary>
public static class AnalyseFichier
{
    /// <summary>Taille lue pour l'analyse : suffisante pour trouver l'en-tête JPEG dans la plupart des cas.</summary>
    public const int TailleEntete = 64 * 1024;

    public static async Task<InfosFichier?> AnalyserAsync(Stream flux, CancellationToken ct = default)
    {
        var tampon = new byte[TailleEntete];
        var lus = 0;
        while (lus < tampon.Length)
        {
            var n = await flux.ReadAsync(tampon.AsMemory(lus), ct);
            if (n == 0) break;
            lus += n;
        }
        if (flux.CanSeek) flux.Position = 0;
        return Analyser(tampon.AsSpan(0, lus));
    }

    public static InfosFichier? Analyser(ReadOnlySpan<byte> d)
    {
        if (d.Length < 12) return null;

        // PDF : « %PDF- »
        if (d[..5].SequenceEqual("%PDF-"u8)) return new InfosFichier("application/pdf", ".pdf", null, null);

        // PNG : 89 50 4E 47 0D 0A 1A 0A, puis IHDR (largeur/hauteur big-endian)
        if (d[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            if (d.Length < 24) return null;
            return new InfosFichier("image/png", ".png",
                (int)BinaryPrimitives.ReadUInt32BigEndian(d[16..20]), (int)BinaryPrimitives.ReadUInt32BigEndian(d[20..24]));
        }

        // WebP : « RIFF » …. « WEBP »
        if (d[..4].SequenceEqual("RIFF"u8) && d[8..12].SequenceEqual("WEBP"u8))
        {
            var (l, h) = DimensionsWebP(d);
            return new InfosFichier("image/webp", ".webp", l, h);
        }

        // JPEG : FF D8 FF
        if (d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF)
        {
            var (l, h) = DimensionsJpeg(d);
            return new InfosFichier("image/jpeg", ".jpg", l, h);
        }

        return null;
    }

    private static (int?, int?) DimensionsWebP(ReadOnlySpan<byte> d)
    {
        if (d.Length < 30) return (null, null);
        var bloc = d[12..16];
        if (bloc.SequenceEqual("VP8 "u8) && d.Length >= 30)
            // Image avec perte : largeur/hauteur sur 14 bits à l'octet 26
            return (BinaryPrimitives.ReadUInt16LittleEndian(d[26..28]) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(d[28..30]) & 0x3FFF);
        if (bloc.SequenceEqual("VP8L"u8) && d.Length >= 25)
        {
            // Sans perte : 14 bits largeur-1, 14 bits hauteur-1, à partir de l'octet 21
            var b = BinaryPrimitives.ReadUInt32LittleEndian(d[21..25]);
            return ((int)(b & 0x3FFF) + 1, (int)((b >> 14) & 0x3FFF) + 1);
        }
        if (bloc.SequenceEqual("VP8X"u8))
            // Étendu : largeur-1 et hauteur-1 sur 24 bits aux octets 24 et 27
            return ((d[24] | d[25] << 8 | d[26] << 16) + 1, (d[27] | d[28] << 8 | d[29] << 16) + 1);
        return (null, null);
    }

    private static (int?, int?) DimensionsJpeg(ReadOnlySpan<byte> d)
    {
        var i = 2;
        while (i + 9 < d.Length)
        {
            if (d[i] != 0xFF) { i++; continue; }
            var marqueur = d[i + 1];
            if (marqueur == 0xD8 || marqueur == 0x01 || marqueur is >= 0xD0 and <= 0xD7) { i += 2; continue; }
            var longueur = BinaryPrimitives.ReadUInt16BigEndian(d[(i + 2)..(i + 4)]);
            // Marqueurs SOFn (hors DHT C4, JPG C8, DAC CC) : hauteur puis largeur
            if (marqueur is >= 0xC0 and <= 0xCF && marqueur is not (0xC4 or 0xC8 or 0xCC))
                return (BinaryPrimitives.ReadUInt16BigEndian(d[(i + 7)..(i + 9)]), BinaryPrimitives.ReadUInt16BigEndian(d[(i + 5)..(i + 7)]));
            i += 2 + longueur;
        }
        return (null, null);
    }
}
