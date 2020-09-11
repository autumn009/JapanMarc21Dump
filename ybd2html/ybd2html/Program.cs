using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ybd2html
{
    class MyField
    {
        internal string id;
        internal string val;
    }

    class Program
    {
        class YBDRecord
        {
            internal MyField[] fields;
            internal string getField(string id)
            {
                return fields.SingleOrDefault(c => c.id == id)?.val;
            }

            internal IEnumerable<string> enumFields(string id)
            {
                return fields.Where(c => c.id == id).Select(c=>c.val).ToArray();
            }
        }


        static void Main(string[] args)
        {
            YBDRecord[] readRecords(string filename)
            {
                bool eof = false;
                using (var reader = File.OpenText(filename))
                {
                    var records = new List<YBDRecord>();
                    for(; ; )
                    {
                        var record = new YBDRecord();

                        var f = new List<MyField>();
                        for(; ; )
                        {
                            var s = reader.ReadLine();
                            if (s == null)
                            {
                                eof = true;
                                break;
                            }
                            if (s.Trim().Length == 0) break;
                            var index = s.IndexOf('\t');
                            if( index < 0 )
                            {
                                Console.WriteLine($"Fatal Exit {s}");
                                Process.GetCurrentProcess().Kill();
                            }
                            var id = s.Substring(0, index);
                            var val = s.Substring(index + 1);
                            f.Add(new MyField() { id = id, val = val });
                        }
                        record.fields = f.ToArray();
                        records.Add(record);
                        if (eof) break;
                    }
                    return records.ToArray();
                }
            }

            if ( args.Length != 2)
            {
                Console.WriteLine("usage: ybd2html YBD_FILE_NAME OUTPUT_DIR_NAME");
                return;
            }

            YBDRecord[] records = readRecords(args[0]);

            var indexHtml = Path.Combine(args[1], "index.html");
            CreateIndexPage(indexHtml);



            var startInfo = new System.Diagnostics.ProcessStartInfo(indexHtml);
            startInfo.UseShellExecute = true;
            System.Diagnostics.Process.Start(startInfo);

            Console.WriteLine("Done.");

            string toHtml(string s)
            {
                var sb = new StringBuilder();
                foreach (var item in s)
                {
                    if (item == '<')
                        sb.Append("&lt;");
                    else if (item == '&')
                        sb.Append("&amp;");
                    else if (item == '"')
                        sb.Append("&quot;");
                    else if (item == '\'')
                        sb.Append("&#039;");
                    else
                        sb.Append(item);
                }
                return sb.ToString();
            }

            void writeHtmlHead(TextWriter writer,string title)
            {
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html lang=\"ja\">");
                writer.WriteLine("<head>");
                writer.WriteLine("<meta charset=\"UTF-8\">");
                writer.WriteLine($"<title>{toHtml(title)}</title>");
                writer.WriteLine("<head>");
                writer.WriteLine("<body>");
            }

            void writeHtmlEnd(TextWriter writer)
            {
                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }

            void writeLink(TextWriter writer, string url, string name )
            {
                writer.Write($"<a href=\"{url}\">{toHtml(name)}</a>");
            }
            void writeLiLink(TextWriter writer, string url, string name)
            {
                writer.Write($"<li><a href=\"{url}\">{toHtml(name)}</a></li>");
            }


            void CreateIndexPage(string path)
            {
                using( TextWriter writer = File.CreateText(path) )
                {
                    writeHtmlHead(writer, "YBD Index");
                    writer.WriteLine("<h1>YBD Index</h1>");

                    writer.WriteLine("<ul>");

                    foreach (var item in records)
                    {
                        var w = "";
                        var wr = item.enumFields("WRITER").ToArray();
                        if (wr.Length > 0) w = wr[0];
                        if (wr.Length > 1) w += "(等)";
                        var p = "";
                        var pr = item.enumFields("PUBLISHER").ToArray();
                        if (pr.Length > 0) p = pr[0];
                        if (pr.Length > 1) p += "(等)";
                        var subject = $"{item.getField("DATE")} {w} {p} {item.getField("SUBJECT")}";
                        writeLiLink(writer, $"{item.getField("YBDID")}.html", subject);
                    }
                    writer.WriteLine("</ul>");
                    writeHtmlEnd(writer);
                    
                }
            }
        }
    }
}
