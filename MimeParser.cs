using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net.Mime;
using System.Collections.Generic;


namespace POP
{
    class MimeParser
    {
        private readonly List<string> lines;
        private readonly string boundary;

        public MimeParser(List<string> messageLines)
        {
            lines = messageLines;

            boundary = ExtractBoundary();
        }

        public string GetBody()
        {
            var body = new List<string>();
            foreach (List<string> section in FindSections())
                body.Add(Decode(section));

            return string.Concat(body);
        }

        private List<List<string>> FindSections()
        {
            var sections = new List<List<string>>();
            var indexes = new List<int>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith(boundary))
                    indexes.Add(i);
            }

            var boundaries = new Dictionary<int, int>();

            for (int i = 0; i < indexes.Count - 1; i++)
            {
                boundaries.Add(indexes[i], indexes[i + 1]);
            }

            foreach (var kv in boundaries)
                sections.Add(lines.GetRange(kv.Key, kv.Value - kv.Key));

            // Sometimes there is a plaintext and a html section

                if (sections.Count < 2)
                    return sections;

            // If the first section is text and the second is HTML then delete the first section because it is 
            // most likely the same thing but in a different format.
            if (sections[0][1].Contains("Content-Type: text/plain;") &&
                sections[1][1].Contains("Content-Type: text/html;"))
                sections.RemoveAt(0);

            return sections;
        }

        private string Decode(List<string> section)
        {
            int encodingIndex = (from line in section
                                 where line.Contains("Content-Transfer-Encoding:")
                                 select section.IndexOf(line)).FirstOrDefault();
            
            string contentEncoding = section[encodingIndex].Split(' ')[1];
            var encoding = TransferEncoding.Unknown;

            if (contentEncoding == "7bit")
                encoding = TransferEncoding.SevenBit;
            //if (contentEncoding == "8bit") Need Framework 4.5
            //    encoding = TransferEncoding.EightBit;
            if (contentEncoding == "quoted-printable")
                encoding = TransferEncoding.QuotedPrintable;
            if (contentEncoding.ToLower() == "base64")
                encoding = TransferEncoding.Base64;

            int space = (from line in section
                         where string.IsNullOrWhiteSpace(line)
                         select section.IndexOf(line)).FirstOrDefault();

            section.RemoveRange(0, space + 1);

            //section.Remove(section[0]); // boundary
            //section.Remove(section[0]); // type
            //section.Remove(section[0]); // transfer encoding

            // Remove '=' at the end of each line
            for(int i = 0; i < section.Count; i++)
            {
                string current = section[i];

                if (current.EndsWith("="))
                    section[i] = current.Substring(0, current.Length - 1);
            }

            MemoryStream ms;
            string lsection = string.Join(string.Empty, section);

            switch (encoding)
            {
                case TransferEncoding.Base64:
                    ms = new MemoryStream(Convert.FromBase64String(lsection), false);
                    break;

                case TransferEncoding.QuotedPrintable:
                    ms = new MemoryStream(Encoding.UTF8.GetBytes(
                       string.Join(string.Empty, (from line in section
                                                  select POP.MailDecoder.DecodeSpecialChars(line)).ToList<string>())
                        ));
                    break;

                //case TransferEncoding.EightBit: Need framework 4.5
                case TransferEncoding.SevenBit:
                case TransferEncoding.Unknown:
                default:
                    ms = new MemoryStream(Encoding.UTF8.GetBytes(lsection), false);
                    break;
            }

            string body;

            using (var sr = new StreamReader(ms))
                body = sr.ReadToEnd();
            ms.Dispose();

            return body;
        }

        private string ExtractBoundary()
        {
            int boundaryIndex = (from l in lines
                                 where l.StartsWith("Content-Type:")
                                 select lines.IndexOf(l)).FirstOrDefault();

            if (!lines[boundaryIndex].Contains("boundary=\""))
                boundaryIndex++;

            string boundaryLine = lines[boundaryIndex];

            if (boundaryLine.StartsWith("\t"))
                boundaryLine = boundaryLine.Substring(1);

            boundaryLine = boundaryLine.Substring(boundaryLine.IndexOf('"') + 1);
            boundaryLine = boundaryLine.Remove(boundaryLine.Length - 1);
            boundaryLine = string.Concat("--", boundaryLine); // MIME logic

            return boundaryLine;
        }
    }
}
