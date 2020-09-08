using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace marcdump
{
    static class FieldDic
    {
        private static Dictionary<string, string> dic = new Dictionary<string, string>();

        internal static bool TryGet(string id, out string result)
        {
            if (dic.TryGetValue(id, out result)) return true;
            result = id;
            dic.Add(id, id);
            return false;
        }

        private static Dictionary<string, bool> visible = new Dictionary<string, bool>();

        internal static bool IsVisibleItem(string v)
        {
            return visible.TryGetValue(v, out bool _);
        }
        static FieldDic()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(@"marcdump.FieldNames.txt")))
            {
                for (; ; )
                {
                    var s = reader.ReadLine();
                    if (s == null) break;
                    var id = s.Substring(0, 4);
                    var val = s.Substring(4);
                    if( val.EndsWith("*"))
                    {
                        visible.Add(id, true);
                        val = val.Substring(0,val.Length-1);
                    }
                    dic.Add(id, val);
                }
            }
        }
    }
}
