namespace Maze.Tools
{
    public interface IAudioManager
    {
        public void SetSoundsEffectsVolume(float volume, bool valueOnDb = false);
        public void SetMusicVolume(float volume, bool valueOnDb = false);
        public float GetMuiscVolume();
        public float GetSfxVolume();
        public void SetPitch(float pitchValue);
        public void SetRandomPitch();
        public void PlaySound(string soundId);
    }

}
