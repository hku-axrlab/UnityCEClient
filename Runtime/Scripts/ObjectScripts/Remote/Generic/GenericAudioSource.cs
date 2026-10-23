using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityCEClient
{
    [RequireComponent(typeof(AudioSource))]
    public class GenericAudioSource : RemoteObject
    {
        private AudioSource audioSource;

        protected override void Awake()
        {
            base.Awake();

            audioSource = GetComponent<AudioSource>();

            valueFunctions.Add("isPlaying", HandleIsPlaying);
            valueFunctions.Add("volume", HandleVolume);
            valueFunctions.Add("normalized_position", HandleNormalizedPosition);
        }

        private void HandleIsPlaying(JToken variableMember)
        {
            bool isPlaying = variableMember.Value<bool>();

            if (audioSource.isPlaying != isPlaying)
            {
                if (isPlaying)
                    audioSource.Play();
                else
                    audioSource.Stop();
            }
        }

        private void HandleVolume(JToken variableMember)
        {
            float volume = variableMember.Value<float>();
            audioSource.volume = volume;
        }

        private void HandleNormalizedPosition(JToken variableMember)
        {
            if (audioSource.clip == null) return;

            float remoteTime = variableMember.Value<float>() * audioSource.clip.length;
            if (Mathf.Abs(audioSource.time - remoteTime) > .25f)
            {
                audioSource.time = remoteTime;
            }
        }
    }
}