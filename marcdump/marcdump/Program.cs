using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
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
        const int SUBFIELD_NUM = 256;  /* サブフィールドの数 */

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
                d.sub[0].data = Encoding.UTF8.GetString(datafield_str, 0, 8);
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
                for (var i = 0; i < e.len-1; i++)
                {
                    if (datafield_str[i] == JPMARC_SF)
                    {
                        /* 各サブデータフィールドを取得 */
                        SubDataField s = new SubDataField(); /* サブデータフィールド */
                        i++;
                        s.id = (char)(datafield_str[i]);
                        int baseofs = i + 1;
                        for (; ; )
                        {
                            i++;
                            if (i >= datafield_str.Length) break;
                            if (datafield_str[i] < 0x20) break;
                        }
                        if (i - 1 - baseofs < 1) break;
                        s.data = Encoding.UTF8.GetString(datafield_str, baseofs, i - baseofs);
                        d.sub[d.num] = s;
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
                int.TryParse(buf, out e.addr);
                return e;
            }

            for (; ; )
            {
                /* レコードラベルの取得 */
                var label = getLabel();
                /* レコードラベルの取得に失敗したら終了 */
                if (label == null) break;
#if DEBUG
                Console.WriteLine(label);
#endif

                /* ディレクトリの取得 */
                var directory = getDir(label);
#if DEBUG
                Console.WriteLine(directory);
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
                Console.WriteLine($"recNum:{recNum}");
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

                /* 出力 */
                for (int i = 0; i < recNum; i++)
                {
                    Console.WriteLine($"{recDirE[i].field} {recDirE[i].len } {recDirE[i].addr }");
                    for (int j = 0; j < recDataD[i].num; j++)
                    {
                        var subrec = recDataD[i].sub[j];
                        Console.WriteLine($"{subrec.id} {subrec.mode} {subrec.data}");
                    }
                }

#if DEBUG
                break;
#endif

            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                Console.WriteLine("Usase: marcdump INPUT_FILE [OUTPUT_FILE]");
                return;
            }
            var srcFileName = args[0];
            var dstWriter = Console.Out;
            if (args.Length == 2)
            {
                dstWriter = new StreamWriter(args[1]);
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
                if (args.Length == 2)
                {
                    dstWriter.Close();
                }
            }
        }
    }
}