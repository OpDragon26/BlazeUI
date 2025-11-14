using System;
using System.Collections.Generic;
using System.Threading;
using LibVLCSharp.Shared;

namespace BlazeUI;

public static class Sound
{
    private static readonly Random random = new Random();
    private static readonly LibVLC libvlc = new();
    private static readonly MediaPlayer mediaPlayer = new(libvlc);

    public static void Init()
    {
        mediaPlayer.Volume = 115;
        Sounds.Move1 = new(libvlc, "assets/sounds/move_1.wav");
        Sounds.Move2 = new(libvlc, "assets/sounds/move_2.wav");
        Sounds.Move3 = new(libvlc, "assets/sounds/move_3.wav");
        Sounds.GameWon = new(libvlc, "assets/sounds/game_won.wav");
        Sounds.GameLost = new(libvlc, "assets/sounds/game_lost.wav");
        
        SoundList = new()
        {
            {"move", new SoundGroup([Sounds.Move1, Sounds.Move2, Sounds.Move3])},
            {"game-won", new SoundSingle(Sounds.GameWon)},
            {"game-lost", new SoundSingle(Sounds.GameLost)},
        };
    }

    private static Dictionary<string, ISound>? SoundList;
    
    public static void PlaySound(string sound)
    {
        Thread t = new Thread((() =>
            {
                if (SoundList!.TryGetValue(sound, out var soundMedia))
                {
                    soundMedia.Play();
                    return;
                }
                throw new KeyNotFoundException($"The given sound '{sound}' was not found.");
            }));
        t.Start();
    }
    
    private static class Sounds
    {
        public static Media? Move1;
        public static Media? Move2;
        public static Media? Move3;
        public static Media? GameWon;
        public static Media? GameLost;
    }

    private class SoundSingle(Media sound) : ISound
    {
        public void Play()
        {
            mediaPlayer.Play(sound);
        }
    }
    
    private class SoundGroup(Media[] sounds) : ISound
    {
        public void Play()
        {
            mediaPlayer.Play(sounds[random.Next(sounds.Length)]);
        }
    }
    
    private interface ISound
    {
        public void Play();
    }
}