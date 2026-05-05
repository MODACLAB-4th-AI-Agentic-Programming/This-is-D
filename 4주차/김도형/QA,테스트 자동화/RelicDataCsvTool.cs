using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Undertaker.Skills;

/// <summary>
/// RelicData ScriptableObject ↔ CSV 임포트/익스포트 도구.
/// 메뉴: Tools > Undertaker > Relic CSV Tool
///
/// [CSV 컬럼 순서]
/// relicName, description, rarity, category, relicType, maxLevel,
/// paramALv1, paramALv2, paramBLv1, paramBLv2
///
/// [Import 규칙]
///   - relicType 값을 고유 키로 사용합니다.
///   - 프로젝트 내 기존 RelicData 에셋 중 relicType이 일치하면 덮어씁니다.
///   - 일치하는 에셋이 없으면 _defaultSavePath 경로에 새로 생성합니다.
///   - icon 필드는 CSV에 포함되지 않습니다 (에디터에서 직접 연결).
/// </summary>
public class RelicDataCsvTool : EditorWindow
{
    // 신규 에셋 생성 시 저장 경로
    private const string DEFAULT_SAVE_PATH = "Assets/Resources/Data/Relics";

    private const string CSV_HEADER =
        "relicName,description,rarity,category,relicType,maxLevel," +
        "paramALv1,paramALv2,paramBLv1,paramBLv2";

    // ── 메뉴 등록 ────────────────────────────────────────────────

    [MenuItem("Tools/Undertaker/Relic CSV Tool")]
    public static void Open() =>
        GetWindow<RelicDataCsvTool>("Relic CSV Tool").minSize = new Vector2(340, 180);

    // ── GUI ──────────────────────────────────────────────────────

    private void OnGUI()
    {
        GUILayout.Label("RelicData ↔ CSV", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        if (GUILayout.Button("📥  Import from CSV", GUILayout.Height(40)))
            RunImport();

        EditorGUILayout.Space(4);

        if (GUILayout.Button("📤  Export to CSV", GUILayout.Height(40)))
            RunExport();

        EditorGUILayout.Space(12);
        EditorGUILayout.HelpBox(
            "Import: relicType을 키로 기존 에셋을 갱신하거나 신규 생성합니다.\n" +
            "Export: 프로젝트 내 모든 RelicData 에셋을 한 파일로 내보냅니다.\n" +
            "icon 필드는 CSV에 포함되지 않습니다.",
            MessageType.Info);
    }

    // ── Import ───────────────────────────────────────────────────

    private static void RunImport()
    {
        string path = EditorUtility.OpenFilePanel("CSV 파일 선택", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length < 2)
        {
            EditorUtility.DisplayDialog("오류", "헤더 외에 데이터 행이 없습니다.", "확인");
            return;
        }

        // 기존 RelicData 에셋을 relicType → 에셋 딕셔너리로 구성
        var existingMap = BuildExistingMap();

        int created = 0, updated = 0, failed = 0;

        // 헤더(line[0]) 스킵, line[1] 부터 처리
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            List<string> cols = ParseCsvLine(lines[i]);
            if (cols.Count < 10)
            {
                Debug.LogWarning($"[RelicCsvTool] {i + 1}행: 컬럼 수 부족 ({cols.Count}/10) — 스킵");
                failed++;
                continue;
            }

            try
            {
                // Enum 파싱
                if (!Enum.TryParse(cols[2], out RelicRarity rarity))
                    throw new Exception($"알 수 없는 rarity: {cols[2]}");
                if (!Enum.TryParse(cols[3], out RelicCategory category))
                    throw new Exception($"알 수 없는 category: {cols[3]}");
                if (!Enum.TryParse(cols[4], out RelicType relicType))
                    throw new Exception($"알 수 없는 relicType: {cols[4]}");

                // 기존 에셋 탐색 또는 신규 생성
                bool isNew = false;
                if (!existingMap.TryGetValue(relicType, out RelicData data))
                {
                    data  = CreateInstance<RelicData>();
                    isNew = true;
                }

                // 필드 적용
                data.relicName        = cols[0];
                data.description      = cols[1];
                data.rarity           = rarity;
                data.category         = category;
                data.relicType        = relicType;
                data.maxLevel         = int.Parse(cols[5]);
                data.paramAPerLevel   = new[] { float.Parse(cols[6]), float.Parse(cols[7]) };
                data.paramBPerLevel   = new[] { float.Parse(cols[8]), float.Parse(cols[9]) };

                if (isNew)
                {
                    // 저장 경로 보장
                    if (!AssetDatabase.IsValidFolder(DEFAULT_SAVE_PATH))
                        Directory.CreateDirectory(
                            Path.Combine(Application.dataPath, DEFAULT_SAVE_PATH["Assets/".Length..]));

                    string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                        $"{DEFAULT_SAVE_PATH}/{relicType}.asset");
                    AssetDatabase.CreateAsset(data, assetPath);
                    existingMap[relicType] = data; // 중복 생성 방지
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(data);
                    updated++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelicCsvTool] {i + 1}행 처리 실패: {e.Message}");
                failed++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Import 완료",
            $"신규 생성: {created}개\n갱신: {updated}개\n실패: {failed}개",
            "확인");
    }

    // ── Export ───────────────────────────────────────────────────

    private static void RunExport()
    {
        string path = EditorUtility.SaveFilePanel(
            "CSV 저장 위치", "", "RelicData", "csv");
        if (string.IsNullOrEmpty(path)) return;

        var allData = FindAllRelicData();
        if (allData.Count == 0)
        {
            EditorUtility.DisplayDialog("알림", "프로젝트 내 RelicData 에셋이 없습니다.", "확인");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(CSV_HEADER);

        foreach (var data in allData)
        {
            float aLv1 = data.paramAPerLevel.Length > 0 ? data.paramAPerLevel[0] : 0f;
            float aLv2 = data.paramAPerLevel.Length > 1 ? data.paramAPerLevel[1] : 0f;
            float bLv1 = data.paramBPerLevel.Length > 0 ? data.paramBPerLevel[0] : 0f;
            float bLv2 = data.paramBPerLevel.Length > 1 ? data.paramBPerLevel[1] : 0f;

            sb.AppendLine(string.Join(",",
                QuoteCsvField(data.relicName),
                QuoteCsvField(data.description),
                data.rarity.ToString(),
                data.category.ToString(),
                data.relicType.ToString(),
                data.maxLevel.ToString(),
                aLv1.ToString("F4"),
                aLv2.ToString("F4"),
                bLv1.ToString("F4"),
                bLv2.ToString("F4")));
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        EditorUtility.DisplayDialog(
            "Export 완료",
            $"{allData.Count}개 항목을 저장했습니다.\n{path}",
            "확인");
    }

    // ── 유틸리티 ─────────────────────────────────────────────────

    /// <summary>프로젝트 내 모든 RelicData를 relicType 기준으로 딕셔너리로 반환합니다.</summary>
    private static Dictionary<RelicType, RelicData> BuildExistingMap()
    {
        var map = new Dictionary<RelicType, RelicData>();
        foreach (var data in FindAllRelicData())
        {
            if (!map.ContainsKey(data.relicType))
                map[data.relicType] = data;
        }
        return map;
    }

    private static List<RelicData> FindAllRelicData()
    {
        var result = new List<RelicData>();
        string[] guids = AssetDatabase.FindAssets("t:RelicData");
        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<RelicData>(assetPath);
            if (data != null) result.Add(data);
        }
        return result;
    }

    /// <summary>
    /// CSV 한 행을 컬럼 리스트로 파싱합니다.
    /// 큰따옴표로 감싼 필드(콤마·줄바꿈 포함 가능)를 올바르게 처리합니다.
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        var cols  = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // 이스케이프된 큰따옴표 ("")
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    cols.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }
        }

        cols.Add(field.ToString());
        return cols;
    }

    /// <summary>필드에 콤마·따옴표·줄바꿈이 포함된 경우 큰따옴표로 감쌉니다.</summary>
    private static string QuoteCsvField(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        bool needsQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n');
        if (!needsQuote) return value;

        return '"' + value.Replace("\"", "\"\"") + '"';
    }
}
