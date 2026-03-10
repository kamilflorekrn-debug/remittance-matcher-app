using System.Runtime.InteropServices;

namespace RemittanceMatcherApp.Services;

public sealed class PdfTextExtractorService
{
    public string ExtractPdfTextViaWord(string pdfPath)
    {
        object? word = null;
        object? doc = null;

        try
        {
            var wordType = Type.GetTypeFromProgID("Word.Application")
                ?? throw new InvalidOperationException("Word nie jest zainstalowany lub COM Word.Application nie jest dostępny.");

            word = Activator.CreateInstance(wordType);
            dynamic w = word!;
            w.Visible = false;

            string text = string.Empty;

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                doc = w.Documents.Open(pdfPath, false, true, false);
                dynamic d = doc!;
                text = (string)(d.Content.Text ?? string.Empty);
                d.Close(false);
                Marshal.FinalReleaseComObject(doc!);
                doc = null;

                if (text.Length >= 200)
                {
                    break;
                }
            }

            w.Quit(false);
            Marshal.FinalReleaseComObject(word);
            return text;
        }
        catch
        {
            try
            {
                if (doc is not null)
                {
                    dynamic d = doc;
                    d.Close(false);
                    Marshal.FinalReleaseComObject(doc);
                }
            }
            catch { }

            try
            {
                if (word is not null)
                {
                    dynamic w = word;
                    w.Quit(false);
                    Marshal.FinalReleaseComObject(word);
                }
            }
            catch { }

            return string.Empty;
        }
    }
}
