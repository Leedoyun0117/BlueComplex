using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 시작 컷신의 눈·TV 잡음을 그리는 텍스처를 코드로 만든다(전용 아트가 아직 없다). 만든 것은 쓴 쪽이 <see cref="Release"/>로 치운다.
    /// 아트가 생기면 이 클래스를 쓰는 곳(<see cref="IntroEyeView"/>, <see cref="IntroNewsView"/>)의 스프라이트만 바꾸면 된다.
    /// </summary>
    internal static class IntroProceduralArt
    {
        /// <summary>아몬드(렌즈) 모양의 흰자위. 위아래 눈꺼풀 가장자리로 갈수록 어두워진다. 가로세로 비율이 달라져도(눈이 뜨이는 동안 높이를 바꾼다) 모양이 유지된다.</summary>
        public static Sprite Almond()
        {
            const int w = 512, h = 256;
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var nx = (x + 0.5f) / w * 2f - 1f;
                    var halfHeight = h * 0.5f * (1f - nx * nx);
                    var dy = Mathf.Abs(y + 0.5f - h * 0.5f);
                    var coverage = Mathf.Clamp01(halfHeight - dy + 0.5f);

                    var t = dy / Mathf.Max(halfHeight, 1f);
                    var shade = 1f - 0.4f * t * t;
                    var g = (byte)Mathf.RoundToInt(255f * shade);
                    pixels[y * w + x] = new Color32(g, g, g, (byte)Mathf.RoundToInt(255f * coverage));
                }
            }

            return ToSprite(w, h, pixels, FilterMode.Bilinear);
        }

        /// <summary>홍채: 가운데는 밝은 파랑, 바깥 테두리로 갈수록 어두운 남색, 방사형 결이 살짝 들어간다.</summary>
        public static Sprite Iris()
        {
            const int size = 256;
            var pixels = new Color32[size * size];
            var center = size * 0.5f;
            var inner = new Color(0.24f, 0.58f, 0.88f);
            var middle = new Color(0.09f, 0.30f, 0.62f);
            var outer = new Color(0.015f, 0.06f, 0.18f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x + 0.5f - center;
                    var dy = y + 0.5f - center;
                    var d = Mathf.Sqrt(dx * dx + dy * dy) / center;
                    var coverage = Mathf.Clamp01((1f - d) * center + 0.5f);

                    var angle = Mathf.Atan2(dy, dx);
                    var streak = 1f + 0.22f * Mathf.Sin(angle * 23f) * Mathf.Sin(angle * 7f + 1.3f);

                    var color = d < 0.6f ? Color.Lerp(inner, middle, d / 0.6f) : Color.Lerp(middle, outer, (d - 0.6f) / 0.4f);
                    color *= streak;
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, coverage);
                }
            }

            return ToSprite(size, size, pixels, FilterMode.Bilinear);
        }

        /// <summary>단색 원반(동공, 하이라이트). 알파만 있고 색은 Image.color로 준다.</summary>
        public static Sprite Disc()
        {
            const int size = 128;
            var pixels = new Color32[size * size];
            var center = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x + 0.5f - center;
                    var dy = y + 0.5f - center;
                    var coverage = Mathf.Clamp01(center - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * coverage));
                }
            }

            return ToSprite(size, size, pixels, FilterMode.Bilinear);
        }

        /// <summary>TV 잡음: 픽셀마다 무작위 회색. RawImage의 uvRect를 흔들어 지직거리게 한다.</summary>
        public static Texture2D Noise()
        {
            const int size = 256;
            var random = new System.Random(926);
            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                var g = (byte)random.Next(0, 256);
                pixels[i] = new Color32(g, g, g, 255);
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>주사선: 4픽셀마다 어두운 줄 두 개. 검정 RawImage에 얹어 세로로 반복시킨다.</summary>
        public static Texture2D Scanlines()
        {
            var texture = new Texture2D(1, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            texture.SetPixels32(new[]
            {
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 110), new Color32(0, 0, 0, 110)
            });
            texture.Apply(false, true);
            return texture;
        }

        public static void Release(Sprite sprite)
        {
            if (sprite == null) return;

            Object.Destroy(sprite.texture);
            Object.Destroy(sprite);
        }

        private static Sprite ToSprite(int width, int height, Color32[] pixels, FilterMode filter)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
