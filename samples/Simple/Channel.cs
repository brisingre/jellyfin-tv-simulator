using System.Collections.Generic;
using System.Text;

[System.Serializable]
public class Vector2
{
	public float x;
	public float y;

	public Vector2(float x, float y)
	{
		this.x = x;
		this.y = y;
	}

	public static Vector2 zero
	{
		get
		{
			return new Vector2(0, 0);
		}
	}
}
[System.Serializable]
public enum EntryType
{
	FILE,
	MANUAL
}

[System.Serializable]
public enum PathType
{
	ABSOLUTE,
	ROOTED,
	LOCAL
}


[System.Serializable]
public class PlaylistEntry 
{
	public EntryType Type = EntryType.FILE;
	public PathType PathType = PathType.ABSOLUTE;
	public string Path;
	public long Length;
	public bool Flagged;

}



[System.Serializable]
public enum FitMode
{
	LETTERBOX = 2,
	CROP = 3,
	STRETCH = 4
}


[System.Serializable]
public class Channel 
{
	public string Name;

	public long TimeCreated;

	public int Seed;

	public bool Randomized = false;

	public int Number;

	public bool Unlisted = false;

	public bool Static = false;

	public bool Filler = true;

	public bool Flagged = false;

	public int ScrambleCodeLevel = 0;

	public Vector2 AspectRatio = Vector2.zero;

	public int SubtitleTrack = 0;

	public int AudioTrack = 1;

	public float VolumePercent = 100;

	public FitMode FitMode = FitMode.LETTERBOX;

	public List<string> Tags = new List<string>();

	public List<PlaylistEntry> playlist = new List<PlaylistEntry>();

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();

		sb.AppendLine(Number.ToString("0000") + " " + Name);
		sb.AppendLine("-----------------------");
		foreach(var entry in playlist)
		{
			sb.AppendLine(entry.Path);
		}

		return sb.ToString();

	}
}
