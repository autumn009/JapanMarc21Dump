﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace marcdump
{
    class Program
    {
        const char JPMARC_RS = '\x1d'; /* レコードセパレータ */
        const char JPMARC_FS = '\x1e'; /* フィールドセパレータ */
        const char JPMARC_SF = '\x1f'; /* サブフィールド識別子の最初の文字 */
        const string FieldIdDate = "260c";   // 出版年月・頒布年月等 ID
        const int SUBFIELD_NUM = 256;  /* サブフィールドの数 */
        private static bool fullMode = false;
        private static bool inverseMode = false;
        private static bool htmlMode = false;
        private static int TotalCounter = 0;    // 検出レコード数
        private static int DateDetectCounter = 0;   // date検出レコード数



        class Entry
        {
            internal string field; /* フィールド識別子 （例: 001, 245 など） */
            internal int len; /* フィールド長 データフィールドの長さ FS含む */
            internal int addr; /* フィールドの先頭文字の位置
	          データフィールド群の先頭からの相対位置*/
        }
        class SubDataField
        {
            /* サブフィールド識別子 */
            internal char id; /* サブフィールド識別文字 （例: A,B,D,Xなど）*/
            internal int datalen; /* データ部の長さ */
            internal int mode; /* データ部のモード （1:ASCII or 2:JIS） */
            /* データ（部）  */
            internal string data; /* 実際のデータ （例: JP, 東京 など）*/
        }

        class DataField
        {
            internal int num; /* データフィールド内にあるサブデータフィールドの数 */
            internal SubDataField[] sub = new SubDataField[SUBFIELD_NUM]; /* サブデータフィールドの配列 */
        }

        private static void dumpBody(TextWriter dstWriter, BinaryReader inputStream)
        {
            string getLabel()
            {
                var labelBin = inputStream.ReadBytes(24);
                if (labelBin.Length != 24) return null;
                return Encoding.UTF8.GetString(labelBin);
            }

            int getBaseAddr(string label)
            {
                var ofs = label.Substring(12, 5);
                if (!int.TryParse(ofs, out int r))
                {
                    Console.WriteLine($"Bad Offset:{ofs}");
                    Process.GetCurrentProcess().Kill();
                }
                return r;
            }

            int getLength(string label)
            {
                var ofs = label.Substring(0, 5);
                if (!int.TryParse(ofs, out int r))
                {
                    Console.WriteLine($"Bad Length:{ofs}");
                    Process.GetCurrentProcess().Kill();
                }
                return r;
            }

            int getDirLen(string label)
            {
                return getBaseAddr(label) - 24;
            }

            string getDir(string label)
            {
                var dirBin = inputStream.ReadBytes(getDirLen(label));
                return Encoding.UTF8.GetString(dirBin);
            }

            byte[] getDataFieldGroup(string label)
            {
                int len = getLength(label) - getBaseAddr(label);
                byte[] bytes = inputStream.ReadBytes(len);
                return bytes;
            }

            /* 001フィールドのデータフィールドの取得 */
            DataField get001DataField(byte[] datafield_str)
            {
                DataField d = new DataField();

                /* サブデータフィールドの数 */
                d.num = 1;

                d.sub = new SubDataField[1];
                d.sub[0] = new SubDataField();

                /* サブフィールド識別文字 （例: A,B,D,Xなど）   */
                /* 注）サブフィールド識別文字はないので適当な値 */
                d.sub[0].id = '1';

                /* データ部の長さ */
                d.sub[0].datalen = 8;

                /* データ部のモード （1:ASCII） */
                d.sub[0].mode = 1;

                /* 実際のデータ （例: 20000001）*/
                //Console.WriteLine(Encoding.UTF8.GetString(datafield_str));
                // 良く分からないがIDが短い場合がある
                //d.sub[0].data = Encoding.UTF8.GetString(datafield_str, 0, 8);
                d.sub[0].data = Encoding.UTF8.GetString(datafield_str, 0, Math.Min(8, datafield_str.Length));
                return d;
            }

#if false
            /* サブデータフィールドの取得 */
            SubDataField getSubField(int addr, byte[] datafield_str)
            {
                SubDataField s = new SubDataField(); /* サブデータフィールド */

                var idbuf = Encoding.UTF8.GetString(, addr + 1, 5);

                /* サブフィールド識別文字 （例: A,B,D,Xなど）   */
                s.id = idbuf[0];

                /* データ部の長さ */
                var buf = idbuf.Substring(1, 3);
                int.TryParse(buf, out s.datalen);

                /* データ部のモード （1:ASCII or 2:JIS） */
                var idbuf2 = idbuf.Substring(4, 1);
                int.TryParse(idbuf2, out s.mode);

                /* データ（部） */
                //var dummy = Encoding.UTF8.GetString(datafield_str);

                s.data = Encoding.UTF8.GetString(datafield_str, addr + 6, s.datalen);
#if false
                if (s.mode == 1)
                { /* ASCIIだったら */
                    ebcdic2ascii(s.data);
                }
                else if (s.mode == 2)
                { /* JISだったら */
                    kanji(s.data);
                }
                else
                { /* どちらでもなかったら強制終了 */
                    printf("!!!!!! NO MODE !!!!!\n");
                    exit(EXIT_FAILURE);
                }
#endif
                return s;
            }
#endif

            /* 001フィールド以外のデータフィールド
 *  （= サブデータフィールドを含むデータフィールド）の取得 */
            DataField getOtherDataField(byte[] datafield_str, Entry e)
            {
                DataField d = new DataField(); /* データフィールド */

                d.num = 0; /* サブデータフィールドの数 */
                for (var i = 0; i < datafield_str.Length - 1; i++)
                {
                    if (datafield_str[i] == JPMARC_SF)
                    {
                        i++;
                        /* 各サブデータフィールドを取得 */
                        SubDataField s = new SubDataField(); /* サブデータフィールド */
                        s.id = (char)(datafield_str[i]);
                        int baseofs = i + 1;
                        for (; ; )
                        {
                            i++;
                            if (i >= datafield_str.Length)
                            {
                                s.data = Encoding.UTF8.GetString(datafield_str, baseofs, i - baseofs);
                                break;
                            }
                            if (datafield_str[i] < 0x20)
                            {
                                s.data = Encoding.UTF8.GetString(datafield_str, baseofs, i - baseofs);
                                i--;    // あとでインクリメントされてしまうので辻褄を合わせる
                                break;
                            }
                        }
                        //if (i - baseofs < 1) break;
                        d.sub[d.num] = s;
                        //i++;
                        d.num++;
                        if (d.num > SUBFIELD_NUM - 1)
                        {
                            Console.WriteLine($"警告：サブフィールドの数が{SUBFIELD_NUM}を越しています。");
                            break;
                        }
                    }
                }
                return d;
            }

            DataField getDataField(byte[] datafieldgroup, Entry e)
            {
                DataField d = new DataField();
                /* データフィールドを文字列として取り出す */
                var datafield_str = new byte[e.len - 1];
                Array.Copy(datafieldgroup, e.addr, datafield_str, 0, e.len - 1);

                /* データフィールドを構造体として取得 */
                if (e.field == "001")
                {
                    /* 001フィールドの場合 */
                    d = get001DataField(datafield_str);
                }
                else
                {
                    /* 001フィールド以外の場合 */
                    d = getOtherDataField(datafield_str, e);
                }
                return d;
            }

            Entry getDirentry(string directory, int index)
            {
                var e = new Entry();
                int addr = 12 * index; /* エントリの先頭位置 */
                string entry = directory.Substring(addr, 12);

                /* フィールド識別子 （例: 001, 245 など）*/
                e.field = entry.Substring(0, 3);

                /* フィールド長: データフィールドの長さ FS含む */
                string buf = entry.Substring(3, 4);
                int.TryParse(buf, out e.len);

                /* フィールドの先頭文字の位置: 
                   データフィールド群の先頭からの相対位置 */
                string buf2 = entry.Substring(7, 5);
                int.TryParse(buf2, out e.addr);
                return e;
            }

            string parseMyDate(string src)
            {
                // TBW
                return src;
            }
            bool numberTester(string s)
            {
                foreach (var item in s)
                {
                    if( item < '0' || item > '9')
                    {
#if DEBUG
                        //Console.WriteLine($"{s} is not number");
#endif
                        return false;
                    }
                }
                return true;
            }

            string parseMyDateBy3(string s1,string s2, string s3)
            {
                if (string.IsNullOrWhiteSpace(s1) || !numberTester(s1)) s1 = "0";
                if (string.IsNullOrWhiteSpace(s2) || !numberTester(s2)) s2 = "0";
                if (string.IsNullOrWhiteSpace(s3) || !numberTester(s3)) s3 = "0";
                var r = s1.PadRight(4, '0') + s2.PadRight(2, '0') + s3.PadRight(2, '0');
#if DEBUG
                //Console.WriteLine($"{r} {s1} {s2} {s3}");
#endif
                return r;
            }


            DateDetectCounter = 0;
            for (; ; )
            {
                /* レコードラベルの取得 */
                var label = getLabel();
                /* レコードラベルの取得に失敗したら終了 */
                if (label == null) break;
#if DEBUG
                //Console.WriteLine(label);
#endif

                /* ディレクトリの取得 */
                var directory = getDir(label);
#if DEBUG
                //Console.WriteLine(directory);
#endif

                /* データフィールド群の取得 */
                var datafieldgroup = getDataFieldGroup(label);

#if DEBUG
                //Console.WriteLine(datafieldgroup);
#endif

                /* 書誌レコードの初期化 */
                var recNum = getDirLen(label) / 12;
                var recDirE = new Entry[recNum];
                var recDataD = new DataField[recNum];
#if DEBUG
                //Console.WriteLine($"recNum:{recNum}");
#endif

                for (int i = 0; i < recNum; i++)
                {
                    /* エントリの取得 */
                    recDirE[i] = getDirentry(directory, i);
                    /* データフィールドの取得 */
                    recDataD[i] = getDataField(datafieldgroup, recDirE[i]);
                }
#if DEBUG
                //Console.WriteLine($"recNum:{recNum}");
#endif

                Dictionary<string, string> duplicateChecker = new Dictionary<string, string>();
                string date = null;
                string f363i = null;
                string f363j = null;
                string f363k = null;
                string f363l = null;
                /* 出力 */
                for (int i = 0; i < recNum; i++)
                {
#if DEBUG
                    //Console.WriteLine($"{recDirE[i].field} {recDirE[i].len } {recDirE[i].addr }");
#endif
                    for (int j = 0; j < recDataD[i].num; j++)
                    {
                        var subrec = recDataD[i].sub[j];
                        var id = $"{recDirE[i].field}{subrec.id}";
#if DEBUG
                        //Console.WriteLine($"{subrec.id} {subrec.mode} {subrec.data}");
#endif

                        var did = $"{id}\t{subrec.data}";
                        if (duplicateChecker.ContainsKey(did)) continue;
                        duplicateChecker.Add(did, null);

                        var visible = inverseMode ^ FieldDic.IsVisibleItem(id);
                        if (fullMode || visible)
                        {
                            var result = FieldDic.TryGet(id, out string category);
                            if (result == false) Console.WriteLine($"category [{category}] not found");

                            if (visible)
                                dstWriter.WriteLine($"{category}\t{subrec.data}");
                            else
                                dstWriter.WriteLine($"({category}\t{subrec.data})");
                        }

                        if (id == FieldIdDate) date = parseMyDate(subrec.data);
                        if (id == "363i") f363i = subrec.data;
                        if (id == "363j") f363j = subrec.data;
                        if (id == "363k") f363k = subrec.data;
                        if (id == "363l") f363l = subrec.data;
                    }
                }


#if DEBUG
                //break;
#endif
                // レコードエンド
                TotalCounter++;
                if (date == null && f363i != null)
                {
                    date = parseMyDateBy3(f363i, f363j, f363k);
                }
                if (date != null)
                {
                    dstWriter.WriteLine($"Detected Date: {date}");
                    DateDetectCounter++;
                }
#if DEBUG
                else
                {
                    dstWriter.WriteLine("!!Missing Date!!");
                }
#endif
                // レコードセパレーター
                dstWriter.WriteLine();
            }
        }

        static private void parseArg(string[] args, out string[] items, out string[] options)
        {
            List<string> itemsList = new List<string>();
            List<string> optionsList = new List<string>();
            foreach (var item in args)
            {
                if (item.StartsWith("-")) optionsList.Add(item); else itemsList.Add(item);
            }
            items = itemsList.ToArray();
            options = optionsList.ToArray();
        }

        static void Main(string[] args)
        {
            string[] rawArgs, options;
            parseArg(args, out rawArgs, out options);
            if (rawArgs.Length == 0 || rawArgs.Length > 2 || options.Contains("-?"))
            {
                usage();
                return;
            }
            fullMode = options.Contains("-f");
            inverseMode = options.Contains("-i");
            htmlMode = options.Contains("-h");
            var srcFileName = rawArgs[0];
            var dstWriter = Console.Out;
            if (rawArgs.Length >= 2)
            {
                dstWriter = new StreamWriter(rawArgs[1]);
            }
            try
            {
                using (var inputStream = new BinaryReader(File.OpenRead(srcFileName)))
                {
                    dumpBody(dstWriter, inputStream);
                }
            }
            finally
            {
                if (rawArgs.Length >= 2)
                {
                    dstWriter.Close();
                }
            }
            Console.WriteLine($"TotalCount: {TotalCounter}");
            Console.WriteLine($"MissingDateCount: {TotalCounter-DateDetectCounter}");
            Console.WriteLine("Done.");
        }

        private static void usage()
        {
            Console.WriteLine("Usase: marcdump INPUT_FILE [OUTPUT_FILE] [-f]");
            Console.WriteLine("use -f option for full-information");
            Console.WriteLine("use -i option for invers information");
            Console.WriteLine("use -h option for output HTML mode");
            Console.WriteLine("use -? option for dump this message");
            return;
        }
    }
}