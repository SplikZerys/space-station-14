using Robust.Client.Audio;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Audio.Components;
using Robust.Shared.Timing;
using FancyWindow = Content.Client.UserInterface.Controls.FancyWindow;

namespace Content.Client._Sunrise.TapePlayer;

[GenerateTypedNameReferences]
public sealed partial class TapePlayerMenu : FancyWindow
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private AudioSystem _audioSystem;

    /// <summary>
    /// Are we currently 'playing' or paused for the play / pause button.
    /// </summary>
    private bool _playState;

    /// <summary>
    /// True if playing, false if paused.
    /// </summary>
    public event Action<bool>? OnPlayPressed;
    public event Action? OnStopPressed;
    public event Action<float>? SetTime;
    public event Action<float>? SetVolume;

    private EntityUid? _audio;

    private float _lockTimer;

    public TapePlayerMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _audioSystem = _entManager.System<AudioSystem>();

        PlayButton.OnPressed += args =>
        {
            OnPlayPressed?.Invoke(!_playState);
        };

        StopButton.OnPressed += args =>
        {
            OnStopPressed?.Invoke();
        };
        PlaybackSlider.OnReleased += PlaybackSliderKeyUp;
        VolumeSlider.OnReleased += VolumeSliderKeyUp;

        SetPlayPauseButton(_audioSystem.IsPlaying(_audio), force: true);
    }

    public TapePlayerMenu(AudioSystem audioSystem)
    {
        _audioSystem = audioSystem;
    }

    public void SetAudioStream(EntityUid? audio)
    {
        _audio = audio;
    }

    public void SetVolumeSlider(float volume)
    {
        VolumeSlider.Value = volume;
    }

    private void PlaybackSliderKeyUp(Slider args)
    {
        SetTime?.Invoke(PlaybackSlider.Value);
        _lockTimer = 0.5f;
    }

    private void VolumeSliderKeyUp(Slider args)
    {
        SetVolume?.Invoke(VolumeSlider.Value / 100f);
        _lockTimer = 0.5f;
    }

    public void SetPlayPauseButton(bool playing, bool force = false)
    {
        if (_playState == playing && !force)
            return;

        _playState = playing;

        if (playing)
        {
            PlayButton.Text = Loc.GetString("jukebox-menu-buttonpause");
            return;
        }

        PlayButton.Text = Loc.GetString("jukebox-menu-buttonplay");
    }

    public void SetSelectedSong(string name, float length)
    {
        SetSelectedSongText(name);
        PlaybackSlider.MaxValue = length;
        PlaybackSlider.SetValueWithoutEvent(0);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_lockTimer > 0f)
        {
            _lockTimer -= args.DeltaSeconds;
        }

        PlaybackSlider.Disabled = _lockTimer > 0f;
        VolumeSlider.Disabled = _lockTimer > 0f;

        if (_entManager.TryGetComponent(_audio, out AudioComponent? audio))
        {
            DurationLabel.Text = $@"{TimeSpan.FromSeconds(audio.PlaybackPosition):mm\:ss} / {_audioSystem.GetAudioLength(audio.FileName):mm\:ss}";
        }
        else
        {
            DurationLabel.Text = $"00:00 / 00:00";
        }

        if (PlaybackSlider.Grabbed)
            return;

        if (audio != null || _entManager.TryGetComponent(_audio, out audio))
        {
            PlaybackSlider.SetValueWithoutEvent(audio.PlaybackPosition);
        }
        else
        {
            PlaybackSlider.SetValueWithoutEvent(0f);
        }

        SetPlayPauseButton(_audioSystem.IsPlaying(_audio, audio));
    }

    public void SetSelectedSongText(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            SongName.Text = text;
        }
        else
        {
            SongName.Text = "---";
        }
    }
}
