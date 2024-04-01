using System;
using System.IO;

namespace Netcode;

public sealed class NetBool : NetField<bool, NetBool>
{
	public NetBool()
	{
	}

	public NetBool(bool value)
		: base(value)
	{
	}

	public override void Set(bool newValue)
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

	protected override void ReadDelta(BinaryReader reader, NetVersion version)
	{
		bool newValue = reader.ReadBoolean();
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
	[Obsolete("Implicitly casting NetBool to bool can have unintuitive behavior. Use the Value field instead.")]
	public static implicit operator bool(NetBool netField)
	{
		return netField?.Value ?? false;
	}
}
