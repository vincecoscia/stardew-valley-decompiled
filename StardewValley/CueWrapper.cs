using System;
using Microsoft.Xna.Framework.Audio;

namespace StardewValley;

public class CueWrapper : ICue, IDisposable
{
	private Cue cue;

	public bool IsStopped => this.cue.IsStopped;

	public bool IsStopping => this.cue.IsStopping;

	public bool IsPlaying => this.cue.IsPlaying;

	public bool IsPaused => this.cue.IsPaused;

	public string Name => this.cue.Name;

	public float Volume
	{
		get
		{
			return this.cue.Volume;
		}
		set
		{
			this.cue.Volume = value;
		}
	}

	public float Pitch
	{
		get
		{
			return this.cue.Pitch;
		}
		set
		{
			this.cue.Pitch = value;
		}
	}

	public bool IsPitchBeingControlledByRPC => this.cue.IsPitchBeingControlledByRPC;

	public CueWrapper(Cue cue)
	{
		this.cue = cue;
	}

	public void Play()
	{
		this.cue.Play();
	}

	public void Pause()
	{
		this.cue.Pause();
	}

	public void Resume()
	{
		this.cue.Resume();
	}

	public void Stop(AudioStopOptions options)
	{
		this.cue.Stop(options);
	}

	public void SetVariable(string var, int val)
	{
		this.cue.SetVariable(var, val);
	}

	public void SetVariable(string var, float val)
	{
		this.cue.SetVariable(var, val);
	}

	public float GetVariable(string var)
	{
		return this.cue.GetVariable(var);
	}

	public void Dispose()
	{
		this.cue.Dispose();
		this.cue = null;
	}
}
