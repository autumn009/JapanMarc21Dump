using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
                var a = fields.SingleOrDefault(c => c.id == id);
                if(a == null)
                {
                    _ = a;
                }
                return a?.val; 
            }

            internal IEnumerable<string> enumFields(string id)
            {
                return fields.Where(c => c.id == id).Select(c => c.val).ToArray();
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
                    for (; ; )
                    {
                        var record = new YBDRecord();

                        var f = new List<MyField>();
                        for (; ; )
                        {
                            var s = reader.ReadLine();
                            if (s == null)
                            {
                                eof = true;
                                break;
                            }
                            if (s.Trim().Length == 0) break;
                            var index = s.IndexOf('\t');
                            if (index < 0)
                            {
                                Console.WriteLine($"Fatal Exit {s}");
                                Process.GetCurrentProcess().Kill();
                            }
                            var id = s.Substring(0, index);
                            var val = s.Substring(index + 1);
                            f.Add(new MyField() { id = id, val = val });
                        }
                        if (f.Count > 0)
                        {
                            record.fields = f.ToArray();
                            records.Add(record);
                        }
                        if (eof) break;
                    }
                    return records.ToArray();
                }
            }

            if (args.Length != 2)
            {
                Console.WriteLine("usage: ybd2html YBD_FILE_NAME OUTPUT_DIR_NAME");
                return;
            }

            YBDRecord[] records = readRecords(args[0]);

            var indexHtml = Path.Combine(args[1], "index.html");
            CreateIndexPage(indexHtml);
            CreateAboutPage(Path.Combine(args[1], "about.html"));

#if false
            var urlPrefix = "http://nan.piedey.co.jp/~autumn/ybd/";
#else
            var urlPrefix = "";
#endif
            var uraHtml = Path.Combine(args[1], "$full.html");
            CreateUraPage(uraHtml);
            foreach (var record in records) CreateEachPage(record, Path.Combine(args[1], record.getField("YBDID") + ".html"));

            var startInfo = new System.Diagnostics.ProcessStartInfo(indexHtml);
            startInfo.UseShellExecute = true;
            System.Diagnostics.Process.Start(startInfo);
            var startInfo2 = new System.Diagnostics.ProcessStartInfo(uraHtml);
            startInfo2.UseShellExecute = true;
            System.Diagnostics.Process.Start(startInfo2);

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

            void writeHtmlHead(TextWriter writer, string title)
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

            void writeLink(TextWriter writer, string url, string name)
            {
                writer.Write($"<a href=\"{url}\">{toHtml(name)}</a>");
            }
            void writeLiLink(TextWriter writer, string url, string name)
            {
                writer.Write($"<li><a href=\"{url}\">{toHtml(name)}</a></li>");
            }

            void writeTable(TextWriter writer, string [] headers, IEnumerable<string[]> enumData)
            {
                writer.WriteLine("<table>");
                writer.WriteLine("<tr>");
                foreach (var item in headers)
                {
                    writer.WriteLine($"<th>{toHtml(item)}</th>");
                }
                writer.WriteLine("</tr>");
                foreach (var item in enumData)
                {
                    writer.WriteLine("<tr>");
                    var url = item.First();
                    foreach (var data in item.Skip(1))
                    {
                        writer.WriteLine($"<td><a href=\"{url}\">{toHtml(data)}</a></td>");
                    }
                    writer.WriteLine("</tr>");
                }
                writer.WriteLine("</table>");
            }

            void CreateLinkToAboutPage(TextWriter writer)
            {
                writer.WriteLine($"<p>[<a href=\"about.html\">このページについて</a>]</p>");
            }

            void CreateLinkToMainPage(TextWriter writer)
            {
                writer.WriteLine($"<p>[<a href=\"index.html\">表紙に戻る</a>]</p>");
            }

            string getDigest(YBDRecord item)
            {
                var w = "";
                var wr = item.enumFields("WRITER").ToArray();
                if (wr.Length > 0) w = wr[0];
                if (wr.Length > 1) w += "(等)";
                var p = "";
                var pr = item.enumFields("PUBLISHER").ToArray();
                if (pr.Length > 0) p = pr[0];
                if (pr.Length > 1) p += "(等)";
                return $"{item.getField("DATE")} {w} {p} {item.getField("SUBJECT")}";
            }

            void CreateIndexPage(string path)
            {
                using (TextWriter writer = File.CreateText(path))
                {
                    writeHtmlHead(writer, "YBD Index");
                    writer.WriteLine("<h1>YBD Index</h1>");

                    writer.WriteLine("<ul>");

                    foreach (var item in records)
                    {
                        writeLiLink(writer, $"{item.getField("YBDID")}.html", getDigest(item));
                    }
                    writer.WriteLine("</ul>");
                    CreateLinkToAboutPage(writer);
                    writeHtmlEnd(writer);
                }
            }

            void CreateEachPage(YBDRecord record, string path)
            {
                using (TextWriter writer = File.CreateText(path))
                {
                    var subject = getDigest(record);

                    writeHtmlHead(writer, subject);
                    writer.WriteLine($"<h1>{toHtml(subject)}</h1>");

                    writer.WriteLine("<table>");
                    writer.WriteLine("<tr>");
                    writer.WriteLine("<th>ID</th>");
                    writer.WriteLine("<th>VALUE</th>");
                    writer.WriteLine("</tr>");

                    foreach (var field in record.fields)
                    {
                        writer.WriteLine("<tr>");
                        writer.WriteLine($"<td>{toHtml(field.id)}</td>");
                        writer.WriteLine($"<td>{toHtml(field.val)}</td>");
                        writer.WriteLine("</tr>");
                    }
                    writer.WriteLine("</table>");
                    CreateLinkToMainPage(writer);
                    CreateLinkToAboutPage(writer);
                    writeHtmlEnd(writer);
                }
            }

            void CreateAboutPage(string path)
            {
                using (TextWriter writer = File.CreateText(path))
                {
                    writeHtmlHead(writer, "About YBD");
                    writer.WriteLine("<h1>About Index</h1>");

                    var assembly = Assembly.GetExecutingAssembly();
                    using (var reader = new StreamReader(assembly.GetManifestResourceStream(@"ybd2html.About.txt")))
                    {
                        for (; ; )
                        {
                            var s = reader.ReadLine();
                            if (s == null) break;
                            writer.Write("<p>");
                            writer.Write(toHtml(s));
                            writer.WriteLine("</p>");
                        }
                    }
                    CreateLinkToMainPage(writer);

                    writeHtmlEnd(writer);
                }
            }

            void CreateUraPage(string path)
            {
                using (TextWriter writer = File.CreateText(path))
                {
                    writeHtmlHead(writer, "FULL INFO");
                    writer.WriteLine("<h1>FULL INFO</h1>");

                    // full index
                    writer.WriteLine("<h2>ALL INDEX</h2>");
                    foreach (var item in records)
                    {
                        writeLiLink(writer, $"{urlPrefix}{item.getField("YBDID")}.html", getDigest(item));
                    }

                    // monthly statics
                    writer.WriteLine("<h2>MONTHLY COUNT</h2>");
                    writer.WriteLine("<table>");
                    writer.WriteLine("<tr>");
                    writer.WriteLine("<th>DATE</th>");
                    writer.WriteLine("<th>VALUE</th>");
                    writer.WriteLine("</tr>");

                    for (int year = 1977; year <= 2020; year++)
                    {
                        for (int month = 1; month <= 12; month++)
                        {
                            writer.WriteLine("<tr>");
                            var ym = year.ToString() + month.ToString().PadLeft(2, '0');
                            writer.WriteLine($"<td>{ym}</td>");
                            writer.WriteLine($"<td>{records.Where(c=>c.getField("DATE").StartsWith(ym)).Count()}</td>");
                            writer.WriteLine("</tr>");
                        }
                    }
                    writer.WriteLine("</table>");

                    // per date list
                    writer.WriteLine("<h2>YEAR COUNT</h2>");
                    writer.WriteLine("<table>");
                    writer.WriteLine("<tr>");
                    writer.WriteLine("<th>DATE</th>");
                    writer.WriteLine("<th>VALUE</th>");
                    writer.WriteLine("</tr>");

                    for (int year = 1977; year <= 2020; year++)
                    {
                        writer.WriteLine("<tr>");
                        var y = year.ToString();
                        writer.WriteLine($"<td>{y}</td>");
                        writer.WriteLine($"<td>{records.Where(c => c.getField("DATE").StartsWith(y)).Count()}</td>");
                        writer.WriteLine("</tr>");
                    }
                    writer.WriteLine("</table>");

                    // per writer list
                    // 著者別リスト
                    Dictionary<string, int> AllWriterNames = new Dictionary<string, int>();

                    foreach (var record in records)
                    {
                        foreach (var item in record.enumFields("WRITER"))
                        {
                            if (AllWriterNames.ContainsKey(item))
                                AllWriterNames[item]++;
                            else
                                AllWriterNames.Add(item, 1);
                        }
                    }


                    foreach (var name in AllWriterNames.OrderByDescending(c => c.Value).ThenBy(c => c.Key))
                    {
                        writer.WriteLine($"<h2>著者 {toHtml(name.Key)} リスト</h2>");
                        writeTable(writer,
                            new string[] { "YBDID", "DATE", "SUBJECT" },
                            records.Where(c => c.enumFields("WRITER").Contains(name.Key)).Select(c =>
                            {
                                string[] values = new string[] {
                                    urlPrefix + c.getField("YBDID")+".html",
                                    c.getField("YBDID"),
                                    c.getField("DATE"),
                                    c.getField("SUBJECT")
                                };
                                return values;
                            }));
                    }

                    // 著者集計リスト
                    writer.WriteLine("著者集計リスト");
                    foreach (var item in AllWriterNames.OrderByDescending(c => c.Value).ThenBy(c => c.Key))
                    {
                        writer.WriteLine($"{item.Key}\t{item.Value}");
                    }
                    // レコードセパレーター
                    writer.WriteLine();

                    // Publisher集計リスト
                    Dictionary<string, int> AllPublisherNames = new Dictionary<string, int>();
                    foreach (var record in records)
                    {
                        foreach (var item in record.enumFields("PUBLISHER"))
                        {
                            if (AllPublisherNames.ContainsKey(item))
                                AllPublisherNames[item]++;
                            else
                                AllPublisherNames.Add(item, 1);
                        }
                    }
                    writer.WriteLine("Publisher集計リスト");
                    foreach (var item in AllPublisherNames.OrderByDescending(c => c.Value).ThenBy(c => c.Key))
                    {
                        writer.WriteLine($"{item.Key}\t{item.Value}");
                    }




                    // per publkisher list




                    writeHtmlEnd(writer);
                }
            }


        }
    }
}
