using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PowerTools
{
    /// <summary>Plays AnimationClips on an Animator through a PlayableGraph (no AnimatorController needed).</summary>
    [RequireComponent(typeof(Animator))]
    public class SpriteAnim : MonoBehaviour
    {
        [SerializeField] AnimationClip m_defaultAnim;

        PlayableGraph _graph;
        AnimationClipPlayable _playable;
        Animator _animator;
        AnimationClip _current;

        public AnimationClip Clip => _current;
        public bool Playing => _graph.IsValid() && _graph.IsPlaying();

        void Awake()
        {
            _animator = GetComponent<Animator>();
            if (m_defaultAnim != null && gameObject.activeInHierarchy) Play(m_defaultAnim);
        }

        public void Play(AnimationClip clip = null, float speed = 1f)
        {
            if (clip == null) clip = m_defaultAnim;
            if (clip == null) return;
            if (_animator == null) _animator = GetComponent<Animator>();
            Stop();
            _graph = PlayableGraph.Create("SpriteAnim " + name);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(_graph, "out", _animator);
            _playable = AnimationClipPlayable.Create(_graph, clip);
            _playable.SetSpeed(speed);
            output.SetSourcePlayable(_playable);
            _graph.Play();
            _current = clip;
        }

        public void Stop()
        {
            if (_graph.IsValid()) _graph.Destroy();
            _current = null;
        }

        public bool IsPlaying(AnimationClip clip = null) => Playing && (clip == null || clip == _current);

        void OnDestroy() { Stop(); }
    }
}
