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
                Console.WriteLine(datafieldgroup);
                break;
#endif

                /* 書誌レコードの初期化 */
                //rec.num = get_dirlen(rec.label) / 12;
                //rec.dir.e = malloc(sizeof(struct entry) * rec.num);
                //rec.data.d =  malloc(sizeof(struct datafield) * rec.num);

                //for (i = 0; i<rec.num; i++){
                /* エントリの取得 */
                //rec.dir.e[i] = get_direntry(directory, i);
                /* データフィールドの取得 */
                //rec.data.d[i] = get_datafield(datafieldgroup, rec.dir.e[i]);
                //}

                /* 出力 */
                //print_record(rec);
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
