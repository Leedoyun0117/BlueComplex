using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// girl_silhouette 스켈레톤 리그(Assets/Animation/girl_silhouette.prefab, SpriteRenderer+Animator 기반)를 창문 자리에 직접 붙여 놓는다.
    /// SpriteRenderer라 Canvas의 RectMask2D로는 클립되지 않는다 — 창문 오른쪽 끝을 정확히 자르진 못한다(자리·크기·페이드는 맞춘다).
    /// 붙이는 즉시 Animator가 기본 상태("Idle")를 재생한다.
    /// </summary>
    internal sealed class IntroGirlRig
    {
        // 이 리그가 쓰는 BlueComplex/Background/Layer 셰이더는 SpriteRenderer.color(정점색)를 읽지 않고 _BaseColor만 읽는다 —
        // 그래서 페이드는 SetAlpha에서 MaterialPropertyBlock으로 _BaseColor.a를 바꿔서 한다.
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly GameObject _instance;
        private readonly SpriteRenderer[] _renderers;
        private readonly float _naturalHeight; // localScale 1일 때 리그 전체 높이(월드 단위).
        private readonly Vector2 _footLocal; // localScale 1·루트가 원점에 있을 때, 실측 바운드의 아랫변 중앙(= "발") 좌표.

        private IntroGirlRig(GameObject instance, SpriteRenderer[] renderers, float naturalHeight, float aspect, Vector2 footLocal)
        {
            _instance = instance;
            _renderers = renderers;
            _naturalHeight = naturalHeight;
            _footLocal = footLocal;
            Aspect = aspect;
        }

        /// <summary>리그의 가로/세로 비율 — 화면에 놓을 자리의 너비를 잡을 때 쓴다.</summary>
        public float Aspect { get; }

        /// <summary>리그 프리팹이 없거나 SpriteRenderer가 없으면 null.</summary>
        public static IntroGirlRig Create(GameObject rigPrefab)
        {
            if (rigPrefab == null) return null;

            var instance = Object.Instantiate(rigPrefab, Vector3.zero, Quaternion.identity);
            instance.name = "IntroGirlRig";

            var renderers = instance.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[IntroGirlRig] girl_silhouette 프리팹에 SpriteRenderer가 없다 — 리그 구조가 바뀌었는지 확인한다.");
                Object.Destroy(instance);
                return null;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var naturalHeight = Mathf.Max(bounds.size.y, 0.01f);
            var aspect = Mathf.Max(bounds.size.x, 0.01f) / naturalHeight;

            // 리그 루트의 원점(0,0,0)이 곧 발밑 중앙이라고 가정하지 않는다 — girl_silhouette.prefab 안에 이미 큰 폭(약 47유닛)의
            // 자식 오프셋이 얹혀 있어서(에디터에서 옮긴 흔적) 그 가정대로 붙이면 화면 밖으로 튕겨 나가 안 보이게 된다.
            // 대신 실측한 바운드의 아랫변 중앙을 "발" 기준점으로 따로 잰다. instance는 (0,0,0)·스케일 1로 막 만들어져 있어 월드 좌표가 곧 루트 로컬 좌표다.
            var footLocal = new Vector2(bounds.center.x, bounds.min.y);

            var animator = instance.GetComponentInChildren<Animator>();
            if (animator != null) animator.Play("Idle", 0, 0f);

            var rig = new IntroGirlRig(instance, renderers, naturalHeight, aspect, footLocal);
            rig.SetAlpha(0f);
            return rig;
        }

        /// <summary><paramref name="parent"/>의 원점(피벗)에 캐릭터의 실측 발밑 중앙(<see cref="_footLocal"/>)이 오도록 붙인다.
        /// <paramref name="parent"/>의 세로 크기(rect.height)만큼 보이도록 리그 크기를 다시 잰다.</summary>
        public void Attach(RectTransform parent)
        {
            var t = _instance.transform;
            t.SetParent(parent, false);
            var scale = parent.rect.height / _naturalHeight;
            t.localScale = Vector3.one * scale;
            t.localRotation = Quaternion.identity;
            t.localPosition = new Vector3(-_footLocal.x * scale, -_footLocal.y * scale, 0f);

            // UI 카메라는 컬링 마스크가 "UI" 레이어 하나뿐이다(UICompositorSetupTool) — 리그가 프리팹의 Default 레이어 그대로면
            // 그 카메라엔 아예 안 잡혀서 자리를 맞춰도 안 보인다. parent(캔버스 계층)와 같은 레이어로 통째로 옮겨 준다.
            SetLayerRecursive(_instance.transform, parent.gameObject.layer);
        }

        private static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (var i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
        }

        public void SetAlpha(float alpha)
        {
            var block = new MaterialPropertyBlock();
            foreach (var renderer in _renderers)
            {
                renderer.GetPropertyBlock(block);
                var color = renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColorId)
                    ? renderer.sharedMaterial.GetColor(BaseColorId)
                    : Color.white;
                color.a = alpha;
                block.SetColor(BaseColorId, color);
                renderer.SetPropertyBlock(block);
            }
        }

        public void Dispose()
        {
            if (_instance != null) Object.Destroy(_instance);
        }
    }
}
