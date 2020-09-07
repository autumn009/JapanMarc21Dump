using System;
using System.ComponentModel.DataAnnotations;
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

            for (; ; )
            {
                /* レコードラベルの取得 */
                var label = getLabel();
                /* レコードラベルの取得に失敗したら終了 */
                if (label == null) break;
#if DEBUG
                Console.WriteLine(label);
                break;
#endif
                /* ディレクトリの取得 */
                //directory = get_dir(directory, get_dirlen(rec.label), fp);

                /* データフィールド群の取得 */
                //datafieldgroup = get_datafieldgroup(datafieldgroup, rec.label, fp);

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
