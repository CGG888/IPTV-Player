using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LibmpvIptvClient.Services
{
    public static class EpgMatcher
    {
        // 常见的垃圾词汇，匹配时应移除
        private static readonly string[] JunkWords = new[] 
        { 
            "HD", "SD", "FHD", "HEVC", "H.264", "H.265", "4K", "1080P", "720P", "50FPS", "60FPS",
            "[高清]", "(高清)", "高清", "标清", "超清", "测试", "试验", "IPV6", "IPV4", "OTT", 
            "LIVE", "STREAM", "CHANNEL" 
        };

        /// <summary>
        /// 尝试匹配频道
        /// </summary>
        /// <param name="sourceName">M3U中的频道名</param>
        /// <param name="epgNames">EPG中所有的标准频道名</param>
        /// <returns>匹配到的EPG名称，未匹配则返回null</returns>
        public static string? Match(string sourceName, IEnumerable<string> epgNames)
        {
            if (string.IsNullOrWhiteSpace(sourceName)) return null;

            // 1. 清洗源名称
            var cleanSource = CleanName(sourceName);
            
            // 2. 尝试在EPG列表中查找
            // 优先完全匹配
            foreach (var epgName in epgNames)
            {
                var cleanEpg = CleanName(epgName);
                if (cleanSource == cleanEpg) return epgName;
            }

            // 3. 智能模糊匹配 (CCTV/卫视规则)
            foreach (var epgName in epgNames)
            {
                if (IsSmartMatch(sourceName, epgName)) return epgName;
            }

            return null;
        }

        /// <summary>
        /// 名称清洗标准化
        /// </summary>
        public static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";

            var s = name.ToUpperInvariant();

            // 移除垃圾词
            foreach (var junk in JunkWords)
            {
                s = s.Replace(junk, "");
            }

            // 移除括号及内容 (e.g. "CCTV-1(备用)" -> "CCTV-1")
            // 修复：转义方括号 \[ \]
            s = Regex.Replace(s, @"\([^\)]*\)|\[[^\]]*\]|【[^】]*】", "");

            // 移除特殊字符
            // 修复：移除非转义的连字符 -
            s = Regex.Replace(s, @"[\s_\.\:\+\-]", "");

            // 中文数字转阿拉伯 (简单处理 CCTV-一 -> CCTV1)
            s = s.Replace("一", "1").Replace("二", "2").Replace("三", "3")
                 .Replace("四", "4").Replace("五", "5").Replace("六", "6")
                 .Replace("七", "7").Replace("八", "8").Replace("九", "9")
                 .Replace("十", "10").Replace("0", "0"); // 0保持

            return s;
        }

        /// <summary>
        /// 智能匹配逻辑
        /// </summary>
        private static bool IsSmartMatch(string src, string dst)
        {
            var cleanSrc = CleanName(src);
            var cleanDst = CleanName(dst);

            // 基础包含检查 (如 "CCTV1" 包含于 "CCTV1")
            // 但要注意: "CCTV1" 包含于 "CCTV10" -> 这是错误的
            
            // 提取数字部分进行严格比对
            var numSrc = ExtractNumbers(cleanSrc);
            var numDst = ExtractNumbers(cleanDst);

            // 如果两者都包含数字，数字必须完全一致
            // 例如: src="CCTV1", dst="CCTV10" -> numSrc={1}, numDst={10} -> 不匹配
            // src="CCTV-1", dst="CCTV1" -> numSrc={1}, numDst={1} -> 匹配
            if (numSrc.Count > 0 || numDst.Count > 0)
            {
                if (!numSrc.SequenceEqual(numDst)) return false;
            }

            // 移除数字后，剩余部分的文本相似度
            var textSrc = Regex.Replace(cleanSrc, @"\d", "");
            var textDst = Regex.Replace(cleanDst, @"\d", "");

            // 剩余文本必须包含 (例如 "CCTV" vs "CCTV")
            // 或者处理 "湖南卫视" vs "湖南"
            if (textSrc == textDst) return true;
            
            // 卫视/台 别名处理
            if (IsStationAlias(textSrc, textDst)) return true;

            return false;
        }

        private static List<int> ExtractNumbers(string s)
        {
            var list = new List<int>();
            var matches = Regex.Matches(s, @"\d+");
            foreach (Match m in matches)
            {
                if (int.TryParse(m.Value, out var v)) list.Add(v);
            }
            return list;
        }

        private static bool IsStationAlias(string a, string b)
        {
            // 归一化: 移除 "卫视", "台", "频道", "TV"
            var suffixes = new[] { "卫视", "电视台", "台", "频道", "TV" };
            foreach (var suf in suffixes)
            {
                a = a.Replace(suf, "");
                b = b.Replace(suf, "");
            }
            return a == b && a.Length > 1; // 至少匹配两个字 (防止 "台" vs "台" 这种空匹配)
        }
    }
}
