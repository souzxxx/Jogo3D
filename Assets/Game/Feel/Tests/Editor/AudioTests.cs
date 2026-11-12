using System.Linq;
using NUnit.Framework;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Feel.Tests
{
    public class AudioTests
    {
        [Test] public void EverySoundIdHasAClip()
        {
            foreach (var id in AudioDirector.Ids)
                Assert.IsNotNull(Resources.Load<AudioClip>("Audio/" + id), "falta o arquivo de som: " + id);
            Assert.IsNotNull(Resources.Load<AudioClip>("Audio/" + AudioDirector.AmbientMusic));
            Assert.IsNotNull(Resources.Load<AudioClip>("Audio/" + AudioDirector.BossMusic));
        }

        [Test] public void NoDuplicateIds() => Assert.AreEqual(AudioDirector.Ids.Length, AudioDirector.Ids.Distinct().Count());

        [Test] public void EffectsAreShortAndMusicLoopsAreLong()
        {
            foreach (var id in AudioDirector.Ids)
                Assert.Less(Resources.Load<AudioClip>("Audio/" + id).length, 3.5f, id + " e longo demais para um efeito");
            Assert.Greater(Resources.Load<AudioClip>("Audio/" + AudioDirector.AmbientMusic).length, 20f);
        }

        [Test] public void VolumeIsClampedAndSteppedInTenths()
        {
            float music = AudioPrefs.Music, effects = AudioPrefs.Effects;
            try
            {
                AudioPrefs.Music = 3f; Assert.AreEqual(1f, AudioPrefs.Music);
                AudioPrefs.Effects = -1f; Assert.AreEqual(0f, AudioPrefs.Effects);
                Assert.AreEqual(7, AudioPrefs.Step(0.7f));
                Assert.AreEqual(10, AudioPrefs.Step(5f));
            }
            finally { AudioPrefs.Music = music; AudioPrefs.Effects = effects; }
        }

        [Test] public void EveryBiomeAmbient_Exists_AndIsLongEnoughToLoop()
        {
            foreach (var id in AudioDirector.AmbientTracks)
            {
                var clip = Resources.Load<AudioClip>("Audio/" + id);
                Assert.IsNotNull(clip, "falta a musica " + id);
                Assert.Greater(clip.length, 20f, id);
            }
            Assert.AreEqual(AudioDirector.AmbientTracks.Length, AudioDirector.AmbientTracks.Distinct().Count());
        }
    }
}
