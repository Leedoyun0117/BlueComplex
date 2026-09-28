using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace BlueComplex.Core.Tests
{
    /// <summary>
    /// 포스트잇 손글씨(런타임에 만드는 PostitStyle.HandFont)를 뺀 모든 글자는 NotoSansKR SDF다.
    /// 테스트 어셈블리가 TMP를 참조하지 않으므로 직렬화된 파일을 글자로 읽어 확인한다.
    /// </summary>
    public class GameFontUnificationTests
    {
        private const string NotoPath = "Fonts/NotoSansKR SDF.asset";
        private const string TmpSettingsPath = "TextMesh Pro/Resources/TMP Settings.asset";

        private static string Guid(string assetPath)
        {
            var meta = File.ReadAllText(Path.Combine(Application.dataPath, assetPath + ".meta"));
            return Regex.Match(meta, @"guid: ([0-9a-f]{32})").Groups[1].Value;
        }

        /// <summary>글꼴을 따로 지정하지 않은 TMP_Text와 RuntimeUi.GameFont는 TMP 기본 글꼴을 쓴다.</summary>
        [Test]
        public void TmpDefaultFont_IsNotoSansKr()
        {
            var settings = File.ReadAllText(Path.Combine(Application.dataPath, TmpSettingsPath));
            var line = Regex.Match(settings, @"m_defaultFontAsset: \{[^}]*\}").Value;

            StringAssert.Contains(Guid(NotoPath), line);
        }

        /// <summary>프리팹·씬에 구워진 TMP 글자는 모두 NotoSansKR SDF를 가리킨다(LiberationSans 등 다른 글꼴 없음).</summary>
        [Test]
        public void SerializedTexts_UseOnlyNotoSansKr()
        {
            var noto = Guid(NotoPath);
            var files = Directory.EnumerateFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".prefab") || f.EndsWith(".unity"));

            var offenders = (from file in files
                             from Match m in Regex.Matches(File.ReadAllText(file), @"m_fontAsset: \{fileID: \d+, guid: ([0-9a-f]{32})")
                             where m.Groups[1].Value != noto
                             select file.Substring(Application.dataPath.Length + 1) + " → " + m.Groups[1].Value).ToList();

            CollectionAssert.IsEmpty(offenders, string.Join("\n", offenders));
        }
    }
}
