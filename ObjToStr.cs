using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Sickhead.Engine.Util;

public static class ObjToStr
{
	private struct ToStringDescription
	{
		public Type Type;

		public List<ToStringMember> Members;
	}

	private struct ToStringMember
	{
		public MemberInfo Member;

		private string _name;

		private string _format;

		public string Name
		{
			get
			{
				if (!string.IsNullOrEmpty(this._name))
				{
					return this._name;
				}
				return this.Member.Name;
			}
			set
			{
				this._name = value;
			}
		}

		public string Format
		{
			get
			{
				if (!string.IsNullOrEmpty(this._format))
				{
					return this._format;
				}
				return "{0}";
			}
			set
			{
				this._format = value;
			}
		}
	}

	public class Style
	{
		public bool ShowRootObjectType;

		public string ObjectDelimiter;

		public string MemberDelimiter;

		public string MemberNameValueDelimiter;

		public bool TrailingNewline;

		public static Style TypeAndMembersSingleLine = new Style
		{
			ShowRootObjectType = true,
			ObjectDelimiter = ":",
			MemberDelimiter = ",",
			MemberNameValueDelimiter = "="
		};

		public static Style MembersOnlyMultiline = new Style
		{
			ShowRootObjectType = false,
			ObjectDelimiter = "",
			MemberDelimiter = "\n",
			MemberNameValueDelimiter = "="
		};

		public Style()
		{
			this.ShowRootObjectType = true;
			this.ObjectDelimiter = ":";
			this.MemberDelimiter = ",";
			this.MemberNameValueDelimiter = "=";
		}
	}

	private static readonly StringBuilder _stringBuilder = new StringBuilder();

	private static readonly Dictionary<Type, ToStringDescription> _cache = new Dictionary<Type, ToStringDescription>();

	public static string Format(object obj, Style style)
	{
		Type type = obj.GetType();
		ObjToStr._cache.Clear();
		if (!ObjToStr._cache.TryGetValue(obj.GetType(), out var desc))
		{
			ToStringDescription toStringDescription = default(ToStringDescription);
			toStringDescription.Type = type;
			toStringDescription.Members = new List<ToStringMember>();
			desc = toStringDescription;
			ObjToStr._cache.Add(type, desc);
			BindingFlags attrs = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			FieldInfo[] fields = type.GetFields(attrs);
			foreach (FieldInfo k in fields)
			{
				ToStringMember toStringMember = default(ToStringMember);
				toStringMember.Member = k;
				toStringMember.Name = k.Name;
				ToStringMember item = toStringMember;
				Type dataType2 = k.GetDataType();
				if (dataType2 == typeof(string))
				{
					item.Format = "\"{0}\"";
				}
				if (dataType2.HasElementType)
				{
					item.Format = "{1}[{2}] {0}";
				}
				desc.Members.Add(item);
			}
			desc.Members.Sort(CompareToStringMembers);
		}
		lock (ObjToStr._stringBuilder)
		{
			ObjToStr._stringBuilder.Clear();
			if (style.ShowRootObjectType)
			{
				ObjToStr._stringBuilder.Append(desc.Type.Name);
				ObjToStr._stringBuilder.Append(style.ObjectDelimiter);
			}
			for (int i = 0; i < desc.Members.Count; i++)
			{
				ToStringMember j = desc.Members[i];
				Type dataType = j.Member.GetDataType();
				object val = j.Member.GetValue(obj);
				ObjToStr._stringBuilder.Append(dataType.Name);
				ObjToStr._stringBuilder.Append(" ");
				ObjToStr._stringBuilder.Append(j.Name);
				ObjToStr._stringBuilder.Append(style.MemberNameValueDelimiter);
				if (val == null)
				{
					ObjToStr._stringBuilder.Append("null");
				}
				else
				{
					Type vtype = val.GetType();
					if (vtype.HasElementType)
					{
						Type etype = vtype.GetElementType();
						string ecount = "?";
						ObjToStr._stringBuilder.AppendFormat(j.Format, val, etype, ecount);
					}
					else
					{
						ObjToStr._stringBuilder.AppendFormat(j.Format, val);
					}
				}
				if (i != desc.Members.Count - 1)
				{
					ObjToStr._stringBuilder.Append(style.MemberDelimiter);
				}
			}
			return ObjToStr._stringBuilder.ToString();
		}
	}

	private static int CompareToStringMembers(ToStringMember a, ToStringMember b)
	{
		return a.Name.CompareTo(b.Name);
	}
}
