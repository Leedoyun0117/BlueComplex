using System.Collections.Generic;
using System.IO;
using BlueComplex.UI.Presentation;
using UnityEditor;
using UnityEngine;

namespace BlueComplex.EditorTools
{
    /// <summary>Assets/Art/UI 아래로 처음 들어오는 PNG를 스프라이트로 임포트한다(이미 설정이 있는 에셋은 건드리지 않는다).</summary>
    public sealed class UiArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/UI/")) return;

            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            Configure(importer);
        }

        /// <summary>UI 스프라이트 임포트 설정. 임포터 코드가 컴파일되기 전에 이미 임포트된 에셋(처음 클론한 프로젝트 등)은 카탈로그 도구가 이걸로 고친다.</summary>
        public static void Configure(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }

    /// <summary>
    /// Assets/Art/UI/Icons/*.png 를 훑어 <see cref="UiIconCatalog"/>(Assets/Resources/UiIconCatalog.asset)를 다시 만든다. id는 파일 이름이다
    /// (아이템 id, 단서 id, "key", "placeholder"). 아이콘을 추가하거나 바꾼 뒤 한 번 돌리면 된다.
    ///
    /// 단서 픽셀아트(Assets/Art/Clue)는 파일 이름이 영문 설명이라 id와 다르다 — <see cref="ClueArt"/> 표가 파일 → 단서 id를 잇고,
    /// 같은 id가 Icons 폴더에 이미 있으면(자리표시 아이콘) 픽셀아트가 그것을 대신한다.
    /// </summary>
    public static class UiIconCatalogTool
    {
        private const string IconFolder = "Assets/Art/UI/Icons";
        private const string ClueArtFolder = "Assets/Art/Clue";
        private const string StageArtFolder = "Assets/Art";
        private const string CatalogPath = "Assets/Resources/UiIconCatalog.asset";

        /// <summary>Assets/Art/Clue 파일 이름(확장자 제외) → 단서 id. 새 단서 그림이 들어오면 한 줄 추가한다.</summary>
        private static readonly (string File, string ClueId)[] ClueArt =
        {
            ("flowers_in_vase", "s1_flower"),
            ("Amusement_park_ticket", "s1_amusement_ticket"),
            ("A_calendar_with_a_specific_dat", "s1_school_calendar"),
            ("A_box_full_of_cookies", "s1_cookie_box"),
            ("Torn_schoolbag", "s1_torn_backpack"),
            ("Novel_with_simple_cover", "s1_horror_novel"),
            ("a_broccoli", "s1_broccoli"),
            ("Worn-out_rabbit_doll", "s1_old_rabbit"),
            ("A_family_photo_of_a_blonde_dau", "s1_family_photo"),
            ("a_notebook_filled_with_creepy", "s1_kids_doodle"),
            ("Landscape_painting_of_a_field", "s1_landscape"),
            ("round_clock", "s1_clock"),
            ("A_document_bearing_a_signatur", "s2_death_notice"),
            // 지원 신청서는 사망 통지서와 같은 그림을 임시로 쓴다 — 별도 그림이 생기면 파일 이름만 바꾼다.
            ("A_document_bearing_a_signatur", "s2_application_form"),
            ("Soft_Icecream", "s2_soft_icecream"),
            // 아래 12종은 생성 도구가 만든 폴더 구조(Room2/<이름>/rotations/<이름>.png) 그대로 두었다 — 그림은 옮기지 않는다.
            ("Room2/A_bowl_filled_with_rainwater/rotations/A_bowl_filled_with_rainwater", "s2_rainwater_bowl"),
            ("Room2/a_glass_of_water/rotations/a_glass_of_water", "s2_water_cup"),
            ("Room2/baguette/rotations/baguette", "s2_baguette"),
            ("Room2/Broken_picture_frame/rotations/Broken_picture_frame", "s2_broken_frame"),
            ("Room2/Clock/rotations/Clock", "s2_clock"),
            ("Room2/crumpled_note/rotations/crumpled_note", "s2_table_note"),
            ("Room2/fountain_pen/rotations/fountain_pen", "s2_fountain_pen"),
            ("Room2/Mail/rotations/Mail", "s2_b_letter"),
            ("Room2/umbrella/rotations/umbrella", "s2_umbrella"),
            ("Room2/Weather_Report_on_TV/rotations/Weather_Report_on_TV", "s2_tv_news"),
            ("Room2/white_chrysanthemum/rotations/white_chrysanthemum", "s2_white_flower"),
            ("Room2/withered_rose/rotations/withered_rose", "s2_withered_flower"),
            // 스테이지 3 12종 — Room3/<이름>/rotations/<이름>.png. s3_clock은 Room2의 Clock과 다른 그림(Room3/Clock)이다.
            ("Room3/An_Old_Letter/rotations/An_Old_Letter", "s3_old_letter"),
            ("Room3/bloody_knife/rotations/bloody_knife", "s3_bloody_knife"),
            ("Room3/bouquet/rotations/bouquet", "s3_bouquet"),
            ("Room3/Clock/rotations/Clock", "s3_clock"),
            ("Room3/mannequin/rotations/mannequin", "s3_mannequin"),
            ("Room3/Map_of_the_UK_with_a_sketch_ma/rotations/Map_of_the_UK_with_a_sketch_ma", "s3_marked_map"),
            ("Room3/old_coat/rotations/old_coat", "s3_old_coat"),
            ("Room3/wet_coat/rotations/wet_coat", "s3_coat"),
            ("Room3/wooden_cremation_urn_with_name/rotations/wooden_cremation_urn_with_name", "s3_urn"),
            ("Room3/A_notebook_full_of_doodles/rotations/A_notebook_full_of_doodles", "s3_scribbled_notebook"),
            ("Room3/A_picture_frame_featuring_the/rotations/A_picture_frame_featuring_the", "s3_picture_frame"),
            // 소프트 아이스크림은 스테이지 2와 같은 원본을 쓴다(의도).
            ("Soft_Icecream", "s3_soft_icecream"),
        };

        /// <summary>스테이지 클리어 연출의 자물쇠·열쇠 그림(Assets/Art 바로 아래) 파일 이름(확장자 제외) → 카탈로그 id. 그림은 제자리에 두고 임포트 설정만 스프라이트(Point)로 맞춘다.</summary>
        private static readonly (string File, string Id)[] StageArt =
        {
            ("lock", StageLockView.LockId),
            ("a_key", StageLockView.KeyId),
        };

        [MenuItem("BlueComplex/UI/Rebuild Icon Catalog")]
        public static void Rebuild()
        {
            ConfigureClueArt();
            ConfigureStageArt();

            // 스프라이트로 임포트되지 않은 PNG가 있으면(임포터가 생기기 전에 들어온 파일) 먼저 고친다 — GUID는 그대로다.
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder }))
            {
                var texturePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(texturePath) is TextureImporter importer)) continue;
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single) continue;

                UiArtImporter.Configure(importer);
                importer.SaveAndReimport();
            }

            var entries = new List<UiIconCatalog.Entry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { IconFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                entries.Add(new UiIconCatalog.Entry { Id = Path.GetFileNameWithoutExtension(path), Sprite = sprite });
            }

            foreach (var (file, clueId) in ClueArt)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ClueArtFolder}/{file}.png");
                if (sprite == null)
                {
                    Debug.LogWarning($"[UiIconCatalogTool] 단서 그림을 스프라이트로 못 읽었다: {ClueArtFolder}/{file}.png (→ {clueId})");
                    continue;
                }

                entries.RemoveAll(e => e.Id == clueId);
                entries.Add(new UiIconCatalog.Entry { Id = clueId, Sprite = sprite });
            }

            foreach (var (file, id) in StageArt)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{StageArtFolder}/{file}.png");
                if (sprite == null)
                {
                    Debug.LogWarning($"[UiIconCatalogTool] 스테이지 클리어 그림을 스프라이트로 못 읽었다: {StageArtFolder}/{file}.png (→ {id})");
                    continue;
                }

                entries.RemoveAll(e => e.Id == id);
                entries.Add(new UiIconCatalog.Entry { Id = id, Sprite = sprite });
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            var catalog = AssetDatabase.LoadAssetAtPath<UiIconCatalog>(CatalogPath);
            if (catalog == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                catalog = ScriptableObject.CreateInstance<UiIconCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetEntries(entries.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UiIconCatalogTool] 아이콘 {entries.Count}개를 카탈로그에 담았다: {string.Join(", ", entries.ConvertAll(e => e.Id))}");
        }

        /// <summary>단서 픽셀아트(64px)를 스프라이트로 만든다. 도트가 번지지 않게 Point 필터, 밉맵·압축 없음.
        /// 이미 스프라이트인 것은 건드리지 않는다 — 손으로 고친 설정을 덮어쓰지 않으려는 것이다.</summary>
        private static void ConfigureClueArt()
        {
            foreach (var (file, _) in ClueArt)
            {
                var path = $"{ClueArtFolder}/{file}.png";
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single) continue;

                UiArtImporter.Configure(importer);
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
        }

        /// <summary>자물쇠·열쇠 그림(64px)을 스프라이트로 만든다 — 단서 픽셀아트와 같은 설정(Point, 밉맵·압축 없음). 이미 스프라이트인 것은 필터만 Point인지 확인한다.
        /// 텍스처 모양(Texture2D)을 명시한다: 처음 임포트되는 PNG가 Cube로 잡혀 스프라이트를 못 읽는 경우가 있었다.</summary>
        private static void ConfigureStageArt()
        {
            foreach (var (file, _) in StageArt)
            {
                var path = $"{StageArtFolder}/{file}.png";
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;

                var isSprite = importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single;
                if (isSprite && importer.filterMode == FilterMode.Point && importer.textureShape == TextureImporterShape.Texture2D) continue;

                UiArtImporter.Configure(importer);
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
        }

        /// <summary>배치모드용 진입점(-executeMethod).</summary>
        public static void RebuildBatch() => Rebuild();
    }
}
