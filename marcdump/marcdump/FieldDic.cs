using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace marcdump
{
    static class FieldDic
    {
        private static Dictionary<string, string> dic = new Dictionary<string, string>()
        {
            {"0011", "レコード管理番号" },
            {"015a", "全国書誌番号" },
            {"020a", "ISBN" },
            {"020c", "入手条件・定価" },
            {"020z", "取り消された又は無効なISBN等" },
            {"0222", "ISSNセンターコード" },
            {"022a", "ISSN" },
            {"028a", "出版者番号" },
            {"028b", "レーベル名" },
            {"035a", "他MARC番号等" },
            {"040a", "レコード作成機関コード" },
            {"040b", "目録用言語コード" },
            {"040c", "レコード変換機関コード" },
            {"041a", "本文の言語" },
            {"084a", "分類記号" },
            {"090a", "請求記号" },
            {"245a", "本タイトル" },
            {"245b", "タイトル関連情報" },
            {"2606", "読みの対応関係" },
            {"260b", "出版者・頒布者等" },
            {"300a", "特定資料種別と資料の数量" },
            {"505a", "内容に関する注記" },
            {"8806", "読みの対応関係" },
            {"880b", "読み" },


        };

        internal static bool TryGet(string id, out string result)
        {
            if (dic.TryGetValue(id, out result)) return true;
            result = id;
            dic.Add(id, id);
            return false;
        }

        private static Dictionary<string, bool> visible = new Dictionary<string, bool>()
        {
            {"020a", true },
            {"020c", true },
            {"020z", true },
            {"0222", true },
            {"022a", true },
            {"028a", true },
            {"035a", true },
            {"245a", true },
            {"245b", true },
            {"260b", true },
            {"300a", true },
            {"505a", true },
        };

        internal static bool IsVisibleItem(string v)
        {
            return visible.TryGetValue(v, out bool _);
        }
    }
}
