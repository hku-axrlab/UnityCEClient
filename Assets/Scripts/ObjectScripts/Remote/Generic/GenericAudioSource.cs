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
        }

        private void HandleIsPlaying(JToken variableMember)
        {
            bool isPlaying = variableMember["value"].Value<bool>();

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
            float volume = variableMember["value"].Value<float>();
            audioSource.volume = volume;
        }
    }
}