using ItemChanger.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace BingoSync.GameUI
{
    internal class AudioPlayer
    {
        private readonly SoundManager soundManager = new(typeof(BingoSync).Assembly, "BingoSync.Resources.Sounds.");
        private readonly GameObject audioGameObject;
        private readonly AudioSource audioSource;
        private readonly List<string> clipNames = ["Beep 1", "Buzz 1", "Buzz 2", "Click 1", "Ping 1", "Ping 2", "Ping 3"];
        public List<string> ClipNames { get { return clipNames; } }
        private readonly List<AudioClip> clips = [];

        public AudioPlayer()
        {
            audioGameObject = new GameObject();
            UnityEngine.Object.DontDestroyOnLoad(audioGameObject);
            audioSource = audioGameObject.AddComponent<AudioSource>();
            foreach (string clipName in clipNames)
            {
                clips.Add(soundManager.GetAudioClip(clipName));
            }
        }

        public void Play(int clip)
        {
            audioSource.PlayOneShot(clips[clip], Controller.GlobalSettings.AudioClipVolume);
        }
    }
}
