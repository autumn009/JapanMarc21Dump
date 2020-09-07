using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Text;

namespace marcdump
{
    class Program
    {
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
            internal SubDataField[] sub = new SubDataField[256]; /* サブデータフィールドの配列 */
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

            string getDataFieldGroup(string label)
            {
                int len = getLength(label) - getBaseAddr(label);
                byte[] bytes = inputStream.ReadBytes(len);
                return Encoding.UTF8.GetString(bytes);
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
                var recDataE = new DataField[recNum];
#if DEBUG
                Console.WriteLine($"recNum:{recNum}");
#endif

                for (int i = 0; i < recNum; i++)
                {
                    /* エントリの取得 */
                    recDirE[i] = getDirentry(directory, i);
                    /* データフィールドの取得 */
                    //rec.data.d[i] = get_datafield(datafieldgroup, rec.dir.e[i]);
                }
#if DEBUG
                //Console.WriteLine($"recNum:{recNum}");
#endif

                /* 出力 */
                for (int i = 0; i < recNum; i++)
                {
                    Console.WriteLine($"{recDirE[i].field} {recDirE[i].len } {recDirE[i].addr }");
                    // TBW
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
