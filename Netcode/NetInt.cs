using System;
using System.IO;

namespace Netcode;

public sealed class NetInt : NetField<int, NetInt>
{
	public NetInt()
	{
	}

	public NetInt(int value)
		: base(value)
	{
	}

	public override void Set(int newValue)
	{
		if (base.canShortcutSet())
		{
			base.value = newValue;
		}
		else if (newValue != base.value)
		{
			base.cleanSet(newValue);
			base.MarkDirty();
		}
	}

	public new bool Equals(NetInt other)
	{
		return base.value == other.value;
	}

	public bool Equals(int other)
	{
		return base.value == other;
	}

	protected override int interpolate(int startValue, int endValue, float factor)
	{
		return startValue + (int)((float)(endValue - startValue) * factor);
	}

	protected override void ReadDelta(BinaryReader reader, NetVersion version)
	{
		int newValue = reader.ReadInt32();
		if (version.IsPriorityOver(base.ChangeVersion))
		{
			base.setInterpolationTarget(newValue);
		}
	}

	protected override void WriteDelta(BinaryWriter writer)
	{
		writer.Write(base.value);
	}

	/// <remarks>Deprecated. Implicit conversion of net fields may cause unneeded copy/allocations or have unintended effects (like null values not equal to null).</remarks>
	[Obsolete("Implicitly casting NetInt to int can have unintuitive behavior. Use the Value field instead.")]
	public static implicit operator int(NetInt netField)
	{
		return netField?.Value ?? 0;
	}
}
