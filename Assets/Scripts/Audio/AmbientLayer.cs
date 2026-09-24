using UnityEngine;

namespace BlueComplex.Audio
{
    /// <summary>
    /// 플레이 내내 깔리는 상시 배경음 한 곡. 심박수 배경음(SoundManager의 bed 소스)과 소스도 상태도 완전히 따로라서
    /// 심박수 구간이 바뀌어도 이 곡은 끊기거나 갈아타지 않는다.
    ///
    /// 루프 이음매: mp3는 인코더 패딩 때문에 AudioSource.loop로 돌리면 이음매에서 틈이나 딸깍 소리가 난다. 그래서 loop를 쓰지 않고
    /// 곡 끝 <c>seam</c>초 전에 같은 곡을 다른 소스에서 처음부터 틀어 서로 크로스페이드한다 — 끝과 처음이 겹쳐 이음매가 안 들린다.
    /// SoundManager.Update가 매 프레임 <see cref="Tick"/>을 부른다.
    /// </summary>
    internal sealed class AmbientLayer
    {
        private readonly AudioSource[] _sources;
        private readonly float[] _gain = new float[2];
        private readonly float[] _goal = new float[2];
        private readonly float[] _fadeSeconds = new float[2];

        private int _active = -1;
        private float _volume;
        private float _seam = 3f;
        private float _lastTime;

        public AmbientLayer(AudioSource a, AudioSource b)
        {
            _sources = new[] { a, b };
            foreach (var source in _sources)
            {
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.loop = false; // 루프는 직접 이어 붙인다.
                source.volume = 0f;
            }
        }

        public bool IsActive => _active >= 0;

        /// <summary>곡을 처음부터 시작한다. 이미 재생 중이면 그 곡은 페이드아웃하고 새로 처음부터 올라온다(재시작).</summary>
        public void Begin(AudioClip clip, float volume, float fadeIn, float seam)
        {
            _volume = volume;
            _seam = Mathf.Min(seam, clip.length * 0.5f);

            if (_active >= 0) Release(_active, fadeIn);
            Launch(_active == 0 ? 1 : 0, clip, fadeIn);
        }

        /// <summary>페이드아웃으로 끈다. 다 사라지면 소스가 멈춘다.</summary>
        public void End(float fadeOut)
        {
            if (_active < 0) return;

            Release(_active, fadeOut);
            _active = -1;
        }

        public void Tick(float deltaTime, float master)
        {
            SeamIfNeeded();

            for (var i = 0; i < _sources.Length; i++)
            {
                var source = _sources[i];
                _gain[i] = Mathf.MoveTowards(_gain[i], _goal[i], _fadeSeconds[i] > 0f ? deltaTime / _fadeSeconds[i] : 1f);
                source.volume = _gain[i] * _volume * master;

                // 완전히 사라진 소스는 멈춘다(재생 위치를 붙들고 있지 않게).
                if (_goal[i] <= 0f && _gain[i] <= 0f && source.isPlaying) source.Stop();
            }
        }

        /// <summary>지금 곡이 끝나기 <c>seam</c>초 전이면 같은 곡을 다른 소스에서 처음부터 올리며 갈아탄다. 끝까지 흘러가 멈춰 버린 경우도 같은 경로로 복구한다.</summary>
        private void SeamIfNeeded()
        {
            if (_active < 0) return;

            var source = _sources[_active];
            var clip = source.clip;
            if (clip == null) return;

            // 끝까지 흘러가 멈춰 버린 경우(프레임 정지 등)는 멈추기 직전에 본 위치가 끝 근처였는지로 가린다 —
            // 아직 로딩 중이라 isPlaying이 false인 것과 구분해야 한다.
            if (source.isPlaying) _lastTime = source.time;
            var nearEnd = _lastTime >= clip.length - _seam;
            if (!nearEnd) return;

            Release(_active, _seam);
            Launch(_active == 0 ? 1 : 0, clip, _seam);
        }

        private void Launch(int slot, AudioClip clip, float fadeIn)
        {
            var source = _sources[slot];
            source.Stop();
            source.clip = clip;
            source.pitch = 1f;
            source.time = 0f;
            _lastTime = 0f;
            source.volume = 0f;
            _gain[slot] = 0f;
            _goal[slot] = 1f;
            _fadeSeconds[slot] = fadeIn;
            _active = slot;
            source.Play();
        }

        private void Release(int slot, float fadeOut)
        {
            _goal[slot] = 0f;
            _fadeSeconds[slot] = fadeOut;
        }
    }
}
