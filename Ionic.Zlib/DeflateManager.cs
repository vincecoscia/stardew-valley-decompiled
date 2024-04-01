using System;

namespace Ionic.Zlib;

internal sealed class DeflateManager
{
	internal delegate BlockState CompressFunc(FlushType flush);

	internal class Config
	{
		internal int GoodLength;

		internal int MaxLazy;

		internal int NiceLength;

		internal int MaxChainLength;

		internal DeflateFlavor Flavor;

		private static readonly Config[] Table;

		private Config(int goodLength, int maxLazy, int niceLength, int maxChainLength, DeflateFlavor flavor)
		{
			this.GoodLength = goodLength;
			this.MaxLazy = maxLazy;
			this.NiceLength = niceLength;
			this.MaxChainLength = maxChainLength;
			this.Flavor = flavor;
		}

		public static Config Lookup(CompressionLevel level)
		{
			return Config.Table[(int)level];
		}

		static Config()
		{
			Config.Table = new Config[10]
			{
				new Config(0, 0, 0, 0, DeflateFlavor.Store),
				new Config(4, 4, 8, 4, DeflateFlavor.Fast),
				new Config(4, 5, 16, 8, DeflateFlavor.Fast),
				new Config(4, 6, 32, 32, DeflateFlavor.Fast),
				new Config(4, 4, 16, 16, DeflateFlavor.Slow),
				new Config(8, 16, 32, 32, DeflateFlavor.Slow),
				new Config(8, 16, 128, 128, DeflateFlavor.Slow),
				new Config(8, 32, 128, 256, DeflateFlavor.Slow),
				new Config(32, 128, 258, 1024, DeflateFlavor.Slow),
				new Config(32, 258, 258, 4096, DeflateFlavor.Slow)
			};
		}
	}

	private static readonly int MEM_LEVEL_MAX = 9;

	private static readonly int MEM_LEVEL_DEFAULT = 8;

	private CompressFunc DeflateFunction;

	private static readonly string[] _ErrorMessage = new string[10] { "need dictionary", "stream end", "", "file error", "stream error", "data error", "insufficient memory", "buffer error", "incompatible version", "" };

	private static readonly int PRESET_DICT = 32;

	private static readonly int INIT_STATE = 42;

	private static readonly int BUSY_STATE = 113;

	private static readonly int FINISH_STATE = 666;

	private static readonly int Z_DEFLATED = 8;

	private static readonly int STORED_BLOCK = 0;

	private static readonly int STATIC_TREES = 1;

	private static readonly int DYN_TREES = 2;

	private static readonly int Z_BINARY = 0;

	private static readonly int Z_ASCII = 1;

	private static readonly int Z_UNKNOWN = 2;

	private static readonly int Buf_size = 16;

	private static readonly int MIN_MATCH = 3;

	private static readonly int MAX_MATCH = 258;

	private static readonly int MIN_LOOKAHEAD = DeflateManager.MAX_MATCH + DeflateManager.MIN_MATCH + 1;

	private static readonly int HEAP_SIZE = 2 * InternalConstants.L_CODES + 1;

	private static readonly int END_BLOCK = 256;

	internal ZlibCodec _codec;

	internal int status;

	internal byte[] pending;

	internal int nextPending;

	internal int pendingCount;

	internal sbyte data_type;

	internal int last_flush;

	internal int w_size;

	internal int w_bits;

	internal int w_mask;

	internal byte[] window;

	internal int window_size;

	internal short[] prev;

	internal short[] head;

	internal int ins_h;

	internal int hash_size;

	internal int hash_bits;

	internal int hash_mask;

	internal int hash_shift;

	internal int block_start;

	private Config config;

	internal int match_length;

	internal int prev_match;

	internal int match_available;

	internal int strstart;

	internal int match_start;

	internal int lookahead;

	internal int prev_length;

	internal CompressionLevel compressionLevel;

	internal CompressionStrategy compressionStrategy;

	internal short[] dyn_ltree;

	internal short[] dyn_dtree;

	internal short[] bl_tree;

	internal Tree treeLiterals = new Tree();

	internal Tree treeDistances = new Tree();

	internal Tree treeBitLengths = new Tree();

	internal short[] bl_count = new short[InternalConstants.MAX_BITS + 1];

	internal int[] heap = new int[2 * InternalConstants.L_CODES + 1];

	internal int heap_len;

	internal int heap_max;

	internal sbyte[] depth = new sbyte[2 * InternalConstants.L_CODES + 1];

	internal int _lengthOffset;

	internal int lit_bufsize;

	internal int last_lit;

	internal int _distanceOffset;

	internal int opt_len;

	internal int static_len;

	internal int matches;

	internal int last_eob_len;

	internal short bi_buf;

	internal int bi_valid;

	private bool Rfc1950BytesEmitted;

	private bool _WantRfc1950HeaderBytes = true;

	internal bool WantRfc1950HeaderBytes
	{
		get
		{
			return this._WantRfc1950HeaderBytes;
		}
		set
		{
			this._WantRfc1950HeaderBytes = value;
		}
	}

	internal DeflateManager()
	{
		this.dyn_ltree = new short[DeflateManager.HEAP_SIZE * 2];
		this.dyn_dtree = new short[(2 * InternalConstants.D_CODES + 1) * 2];
		this.bl_tree = new short[(2 * InternalConstants.BL_CODES + 1) * 2];
	}

	private void _InitializeLazyMatch()
	{
		this.window_size = 2 * this.w_size;
		Array.Clear(this.head, 0, this.hash_size);
		this.config = Config.Lookup(this.compressionLevel);
		this.SetDeflater();
		this.strstart = 0;
		this.block_start = 0;
		this.lookahead = 0;
		this.match_length = (this.prev_length = DeflateManager.MIN_MATCH - 1);
		this.match_available = 0;
		this.ins_h = 0;
	}

	private void _InitializeTreeData()
	{
		this.treeLiterals.dyn_tree = this.dyn_ltree;
		this.treeLiterals.staticTree = StaticTree.Literals;
		this.treeDistances.dyn_tree = this.dyn_dtree;
		this.treeDistances.staticTree = StaticTree.Distances;
		this.treeBitLengths.dyn_tree = this.bl_tree;
		this.treeBitLengths.staticTree = StaticTree.BitLengths;
		this.bi_buf = 0;
		this.bi_valid = 0;
		this.last_eob_len = 8;
		this._InitializeBlocks();
	}

	internal void _InitializeBlocks()
	{
		for (int i = 0; i < InternalConstants.L_CODES; i++)
		{
			this.dyn_ltree[i * 2] = 0;
		}
		for (int j = 0; j < InternalConstants.D_CODES; j++)
		{
			this.dyn_dtree[j * 2] = 0;
		}
		for (int k = 0; k < InternalConstants.BL_CODES; k++)
		{
			this.bl_tree[k * 2] = 0;
		}
		this.dyn_ltree[DeflateManager.END_BLOCK * 2] = 1;
		this.opt_len = (this.static_len = 0);
		this.last_lit = (this.matches = 0);
	}

	internal void pqdownheap(short[] tree, int k)
	{
		int v = this.heap[k];
		for (int i = k << 1; i <= this.heap_len; i <<= 1)
		{
			if (i < this.heap_len && DeflateManager._IsSmaller(tree, this.heap[i + 1], this.heap[i], this.depth))
			{
				i++;
			}
			if (DeflateManager._IsSmaller(tree, v, this.heap[i], this.depth))
			{
				break;
			}
			this.heap[k] = this.heap[i];
			k = i;
		}
		this.heap[k] = v;
	}

	internal static bool _IsSmaller(short[] tree, int n, int m, sbyte[] depth)
	{
		short tn2 = tree[n * 2];
		short tm2 = tree[m * 2];
		if (tn2 >= tm2)
		{
			if (tn2 == tm2)
			{
				return depth[n] <= depth[m];
			}
			return false;
		}
		return true;
	}

	internal void scan_tree(short[] tree, int max_code)
	{
		int prevlen = -1;
		int nextlen = tree[1];
		int count = 0;
		int max_count = 7;
		int min_count = 4;
		if (nextlen == 0)
		{
			max_count = 138;
			min_count = 3;
		}
		tree[(max_code + 1) * 2 + 1] = short.MaxValue;
		for (int i = 0; i <= max_code; i++)
		{
			int curlen = nextlen;
			nextlen = tree[(i + 1) * 2 + 1];
			if (++count < max_count && curlen == nextlen)
			{
				continue;
			}
			if (count < min_count)
			{
				this.bl_tree[curlen * 2] = (short)(this.bl_tree[curlen * 2] + count);
			}
			else if (curlen != 0)
			{
				if (curlen != prevlen)
				{
					this.bl_tree[curlen * 2]++;
				}
				this.bl_tree[InternalConstants.REP_3_6 * 2]++;
			}
			else if (count <= 10)
			{
				this.bl_tree[InternalConstants.REPZ_3_10 * 2]++;
			}
			else
			{
				this.bl_tree[InternalConstants.REPZ_11_138 * 2]++;
			}
			count = 0;
			prevlen = curlen;
			if (nextlen == 0)
			{
				max_count = 138;
				min_count = 3;
			}
			else if (curlen == nextlen)
			{
				max_count = 6;
				min_count = 3;
			}
			else
			{
				max_count = 7;
				min_count = 4;
			}
		}
	}

	internal int build_bl_tree()
	{
		this.scan_tree(this.dyn_ltree, this.treeLiterals.max_code);
		this.scan_tree(this.dyn_dtree, this.treeDistances.max_code);
		this.treeBitLengths.build_tree(this);
		int max_blindex = InternalConstants.BL_CODES - 1;
		while (max_blindex >= 3 && this.bl_tree[Tree.bl_order[max_blindex] * 2 + 1] == 0)
		{
			max_blindex--;
		}
		this.opt_len += 3 * (max_blindex + 1) + 5 + 5 + 4;
		return max_blindex;
	}

	internal void send_all_trees(int lcodes, int dcodes, int blcodes)
	{
		this.send_bits(lcodes - 257, 5);
		this.send_bits(dcodes - 1, 5);
		this.send_bits(blcodes - 4, 4);
		for (int rank = 0; rank < blcodes; rank++)
		{
			this.send_bits(this.bl_tree[Tree.bl_order[rank] * 2 + 1], 3);
		}
		this.send_tree(this.dyn_ltree, lcodes - 1);
		this.send_tree(this.dyn_dtree, dcodes - 1);
	}

	internal void send_tree(short[] tree, int max_code)
	{
		int prevlen = -1;
		int nextlen = tree[1];
		int count = 0;
		int max_count = 7;
		int min_count = 4;
		if (nextlen == 0)
		{
			max_count = 138;
			min_count = 3;
		}
		for (int i = 0; i <= max_code; i++)
		{
			int curlen = nextlen;
			nextlen = tree[(i + 1) * 2 + 1];
			if (++count < max_count && curlen == nextlen)
			{
				continue;
			}
			if (count < min_count)
			{
				do
				{
					this.send_code(curlen, this.bl_tree);
				}
				while (--count != 0);
			}
			else if (curlen != 0)
			{
				if (curlen != prevlen)
				{
					this.send_code(curlen, this.bl_tree);
					count--;
				}
				this.send_code(InternalConstants.REP_3_6, this.bl_tree);
				this.send_bits(count - 3, 2);
			}
			else if (count <= 10)
			{
				this.send_code(InternalConstants.REPZ_3_10, this.bl_tree);
				this.send_bits(count - 3, 3);
			}
			else
			{
				this.send_code(InternalConstants.REPZ_11_138, this.bl_tree);
				this.send_bits(count - 11, 7);
			}
			count = 0;
			prevlen = curlen;
			if (nextlen == 0)
			{
				max_count = 138;
				min_count = 3;
			}
			else if (curlen == nextlen)
			{
				max_count = 6;
				min_count = 3;
			}
			else
			{
				max_count = 7;
				min_count = 4;
			}
		}
	}

	private void put_bytes(byte[] p, int start, int len)
	{
		Array.Copy(p, start, this.pending, this.pendingCount, len);
		this.pendingCount += len;
	}

	internal void send_code(int c, short[] tree)
	{
		int c2 = c * 2;
		this.send_bits(tree[c2] & 0xFFFF, tree[c2 + 1] & 0xFFFF);
	}

	internal void send_bits(int value, int length)
	{
		if (this.bi_valid > DeflateManager.Buf_size - length)
		{
			this.bi_buf |= (short)((value << this.bi_valid) & 0xFFFF);
			this.pending[this.pendingCount++] = (byte)this.bi_buf;
			this.pending[this.pendingCount++] = (byte)(this.bi_buf >> 8);
			this.bi_buf = (short)(value >>> DeflateManager.Buf_size - this.bi_valid);
			this.bi_valid += length - DeflateManager.Buf_size;
		}
		else
		{
			this.bi_buf |= (short)((value << this.bi_valid) & 0xFFFF);
			this.bi_valid += length;
		}
	}

	internal void _tr_align()
	{
		this.send_bits(DeflateManager.STATIC_TREES << 1, 3);
		this.send_code(DeflateManager.END_BLOCK, StaticTree.lengthAndLiteralsTreeCodes);
		this.bi_flush();
		if (1 + this.last_eob_len + 10 - this.bi_valid < 9)
		{
			this.send_bits(DeflateManager.STATIC_TREES << 1, 3);
			this.send_code(DeflateManager.END_BLOCK, StaticTree.lengthAndLiteralsTreeCodes);
			this.bi_flush();
		}
		this.last_eob_len = 7;
	}

	internal bool _tr_tally(int dist, int lc)
	{
		this.pending[this._distanceOffset + this.last_lit * 2] = (byte)((uint)dist >> 8);
		this.pending[this._distanceOffset + this.last_lit * 2 + 1] = (byte)dist;
		this.pending[this._lengthOffset + this.last_lit] = (byte)lc;
		this.last_lit++;
		if (dist == 0)
		{
			this.dyn_ltree[lc * 2]++;
		}
		else
		{
			this.matches++;
			dist--;
			this.dyn_ltree[(Tree.LengthCode[lc] + InternalConstants.LITERALS + 1) * 2]++;
			this.dyn_dtree[Tree.DistanceCode(dist) * 2]++;
		}
		if ((this.last_lit & 0x1FFF) == 0 && this.compressionLevel > CompressionLevel.Level2)
		{
			int out_length = this.last_lit << 3;
			int in_length = this.strstart - this.block_start;
			for (int dcode = 0; dcode < InternalConstants.D_CODES; dcode++)
			{
				out_length = (int)(out_length + this.dyn_dtree[dcode * 2] * (5L + (long)Tree.ExtraDistanceBits[dcode]));
			}
			out_length >>= 3;
			if (this.matches < this.last_lit / 2 && out_length < in_length / 2)
			{
				return true;
			}
		}
		if (this.last_lit != this.lit_bufsize - 1)
		{
			return this.last_lit == this.lit_bufsize;
		}
		return true;
	}

	internal void send_compressed_block(short[] ltree, short[] dtree)
	{
		int lx = 0;
		if (this.last_lit != 0)
		{
			do
			{
				int ix = this._distanceOffset + lx * 2;
				int distance = ((this.pending[ix] << 8) & 0xFF00) | (this.pending[ix + 1] & 0xFF);
				int lc = this.pending[this._lengthOffset + lx] & 0xFF;
				lx++;
				if (distance == 0)
				{
					this.send_code(lc, ltree);
					continue;
				}
				int code = Tree.LengthCode[lc];
				this.send_code(code + InternalConstants.LITERALS + 1, ltree);
				int extra = Tree.ExtraLengthBits[code];
				if (extra != 0)
				{
					lc -= Tree.LengthBase[code];
					this.send_bits(lc, extra);
				}
				distance--;
				code = Tree.DistanceCode(distance);
				this.send_code(code, dtree);
				extra = Tree.ExtraDistanceBits[code];
				if (extra != 0)
				{
					distance -= Tree.DistanceBase[code];
					this.send_bits(distance, extra);
				}
			}
			while (lx < this.last_lit);
		}
		this.send_code(DeflateManager.END_BLOCK, ltree);
		this.last_eob_len = ltree[DeflateManager.END_BLOCK * 2 + 1];
	}

	internal void set_data_type()
	{
		int i = 0;
		int ascii_freq = 0;
		int bin_freq = 0;
		for (; i < 7; i++)
		{
			bin_freq += this.dyn_ltree[i * 2];
		}
		for (; i < 128; i++)
		{
			ascii_freq += this.dyn_ltree[i * 2];
		}
		for (; i < InternalConstants.LITERALS; i++)
		{
			bin_freq += this.dyn_ltree[i * 2];
		}
		this.data_type = (sbyte)((bin_freq > ascii_freq >> 2) ? DeflateManager.Z_BINARY : DeflateManager.Z_ASCII);
	}

	internal void bi_flush()
	{
		if (this.bi_valid == 16)
		{
			this.pending[this.pendingCount++] = (byte)this.bi_buf;
			this.pending[this.pendingCount++] = (byte)(this.bi_buf >> 8);
			this.bi_buf = 0;
			this.bi_valid = 0;
		}
		else if (this.bi_valid >= 8)
		{
			this.pending[this.pendingCount++] = (byte)this.bi_buf;
			this.bi_buf >>= 8;
			this.bi_valid -= 8;
		}
	}

	internal void bi_windup()
	{
		if (this.bi_valid > 8)
		{
			this.pending[this.pendingCount++] = (byte)this.bi_buf;
			this.pending[this.pendingCount++] = (byte)(this.bi_buf >> 8);
		}
		else if (this.bi_valid > 0)
		{
			this.pending[this.pendingCount++] = (byte)this.bi_buf;
		}
		this.bi_buf = 0;
		this.bi_valid = 0;
	}

	internal void copy_block(int buf, int len, bool header)
	{
		this.bi_windup();
		this.last_eob_len = 8;
		if (header)
		{
			this.pending[this.pendingCount++] = (byte)len;
			this.pending[this.pendingCount++] = (byte)(len >> 8);
			this.pending[this.pendingCount++] = (byte)(~len);
			this.pending[this.pendingCount++] = (byte)(~len >> 8);
		}
		this.put_bytes(this.window, buf, len);
	}

	internal void flush_block_only(bool eof)
	{
		this._tr_flush_block((this.block_start >= 0) ? this.block_start : (-1), this.strstart - this.block_start, eof);
		this.block_start = this.strstart;
		this._codec.flush_pending();
	}

	internal BlockState DeflateNone(FlushType flush)
	{
		int max_block_size = 65535;
		if (max_block_size > this.pending.Length - 5)
		{
			max_block_size = this.pending.Length - 5;
		}
		while (true)
		{
			if (this.lookahead <= 1)
			{
				this._fillWindow();
				if (this.lookahead == 0 && flush == FlushType.None)
				{
					return BlockState.NeedMore;
				}
				if (this.lookahead == 0)
				{
					break;
				}
			}
			this.strstart += this.lookahead;
			this.lookahead = 0;
			int max_start = this.block_start + max_block_size;
			if (this.strstart == 0 || this.strstart >= max_start)
			{
				this.lookahead = this.strstart - max_start;
				this.strstart = max_start;
				this.flush_block_only(eof: false);
				if (this._codec.AvailableBytesOut == 0)
				{
					return BlockState.NeedMore;
				}
			}
			if (this.strstart - this.block_start >= this.w_size - DeflateManager.MIN_LOOKAHEAD)
			{
				this.flush_block_only(eof: false);
				if (this._codec.AvailableBytesOut == 0)
				{
					return BlockState.NeedMore;
				}
			}
		}
		this.flush_block_only(flush == FlushType.Finish);
		if (this._codec.AvailableBytesOut == 0)
		{
			if (flush != FlushType.Finish)
			{
				return BlockState.NeedMore;
			}
			return BlockState.FinishStarted;
		}
		if (flush != FlushType.Finish)
		{
			return BlockState.BlockDone;
		}
		return BlockState.FinishDone;
	}

	internal void _tr_stored_block(int buf, int stored_len, bool eof)
	{
		this.send_bits((DeflateManager.STORED_BLOCK << 1) + (eof ? 1 : 0), 3);
		this.copy_block(buf, stored_len, header: true);
	}

	internal void _tr_flush_block(int buf, int stored_len, bool eof)
	{
		int max_blindex = 0;
		int opt_lenb;
		int static_lenb;
		if (this.compressionLevel > CompressionLevel.None)
		{
			if (this.data_type == DeflateManager.Z_UNKNOWN)
			{
				this.set_data_type();
			}
			this.treeLiterals.build_tree(this);
			this.treeDistances.build_tree(this);
			max_blindex = this.build_bl_tree();
			opt_lenb = this.opt_len + 3 + 7 >> 3;
			static_lenb = this.static_len + 3 + 7 >> 3;
			if (static_lenb <= opt_lenb)
			{
				opt_lenb = static_lenb;
			}
		}
		else
		{
			opt_lenb = (static_lenb = stored_len + 5);
		}
		if (stored_len + 4 <= opt_lenb && buf != -1)
		{
			this._tr_stored_block(buf, stored_len, eof);
		}
		else if (static_lenb == opt_lenb)
		{
			this.send_bits((DeflateManager.STATIC_TREES << 1) + (eof ? 1 : 0), 3);
			this.send_compressed_block(StaticTree.lengthAndLiteralsTreeCodes, StaticTree.distTreeCodes);
		}
		else
		{
			this.send_bits((DeflateManager.DYN_TREES << 1) + (eof ? 1 : 0), 3);
			this.send_all_trees(this.treeLiterals.max_code + 1, this.treeDistances.max_code + 1, max_blindex + 1);
			this.send_compressed_block(this.dyn_ltree, this.dyn_dtree);
		}
		this._InitializeBlocks();
		if (eof)
		{
			this.bi_windup();
		}
	}

	private void _fillWindow()
	{
		do
		{
			int more = this.window_size - this.lookahead - this.strstart;
			int j;
			if (more == 0 && this.strstart == 0 && this.lookahead == 0)
			{
				more = this.w_size;
			}
			else if (more == -1)
			{
				more--;
			}
			else if (this.strstart >= this.w_size + this.w_size - DeflateManager.MIN_LOOKAHEAD)
			{
				Array.Copy(this.window, this.w_size, this.window, 0, this.w_size);
				this.match_start -= this.w_size;
				this.strstart -= this.w_size;
				this.block_start -= this.w_size;
				j = this.hash_size;
				int p = j;
				do
				{
					int i = this.head[--p] & 0xFFFF;
					this.head[p] = (short)((i >= this.w_size) ? (i - this.w_size) : 0);
				}
				while (--j != 0);
				j = this.w_size;
				p = j;
				do
				{
					int i = this.prev[--p] & 0xFFFF;
					this.prev[p] = (short)((i >= this.w_size) ? (i - this.w_size) : 0);
				}
				while (--j != 0);
				more += this.w_size;
			}
			if (this._codec.AvailableBytesIn == 0)
			{
				break;
			}
			j = this._codec.read_buf(this.window, this.strstart + this.lookahead, more);
			this.lookahead += j;
			if (this.lookahead >= DeflateManager.MIN_MATCH)
			{
				this.ins_h = this.window[this.strstart] & 0xFF;
				this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + 1] & 0xFF)) & this.hash_mask;
			}
		}
		while (this.lookahead < DeflateManager.MIN_LOOKAHEAD && this._codec.AvailableBytesIn != 0);
	}

	internal BlockState DeflateFast(FlushType flush)
	{
		int hash_head = 0;
		while (true)
		{
			if (this.lookahead < DeflateManager.MIN_LOOKAHEAD)
			{
				this._fillWindow();
				if (this.lookahead < DeflateManager.MIN_LOOKAHEAD && flush == FlushType.None)
				{
					return BlockState.NeedMore;
				}
				if (this.lookahead == 0)
				{
					break;
				}
			}
			if (this.lookahead >= DeflateManager.MIN_MATCH)
			{
				this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + (DeflateManager.MIN_MATCH - 1)] & 0xFF)) & this.hash_mask;
				hash_head = this.head[this.ins_h] & 0xFFFF;
				this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
				this.head[this.ins_h] = (short)this.strstart;
			}
			if (hash_head != 0L && ((this.strstart - hash_head) & 0xFFFF) <= this.w_size - DeflateManager.MIN_LOOKAHEAD && this.compressionStrategy != CompressionStrategy.HuffmanOnly)
			{
				this.match_length = this.longest_match(hash_head);
			}
			bool bflush;
			if (this.match_length >= DeflateManager.MIN_MATCH)
			{
				bflush = this._tr_tally(this.strstart - this.match_start, this.match_length - DeflateManager.MIN_MATCH);
				this.lookahead -= this.match_length;
				if (this.match_length <= this.config.MaxLazy && this.lookahead >= DeflateManager.MIN_MATCH)
				{
					this.match_length--;
					do
					{
						this.strstart++;
						this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + (DeflateManager.MIN_MATCH - 1)] & 0xFF)) & this.hash_mask;
						hash_head = this.head[this.ins_h] & 0xFFFF;
						this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
						this.head[this.ins_h] = (short)this.strstart;
					}
					while (--this.match_length != 0);
					this.strstart++;
				}
				else
				{
					this.strstart += this.match_length;
					this.match_length = 0;
					this.ins_h = this.window[this.strstart] & 0xFF;
					this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + 1] & 0xFF)) & this.hash_mask;
				}
			}
			else
			{
				bflush = this._tr_tally(0, this.window[this.strstart] & 0xFF);
				this.lookahead--;
				this.strstart++;
			}
			if (bflush)
			{
				this.flush_block_only(eof: false);
				if (this._codec.AvailableBytesOut == 0)
				{
					return BlockState.NeedMore;
				}
			}
		}
		this.flush_block_only(flush == FlushType.Finish);
		if (this._codec.AvailableBytesOut == 0)
		{
			if (flush == FlushType.Finish)
			{
				return BlockState.FinishStarted;
			}
			return BlockState.NeedMore;
		}
		if (flush != FlushType.Finish)
		{
			return BlockState.BlockDone;
		}
		return BlockState.FinishDone;
	}

	internal BlockState DeflateSlow(FlushType flush)
	{
		int hash_head = 0;
		while (true)
		{
			if (this.lookahead < DeflateManager.MIN_LOOKAHEAD)
			{
				this._fillWindow();
				if (this.lookahead < DeflateManager.MIN_LOOKAHEAD && flush == FlushType.None)
				{
					return BlockState.NeedMore;
				}
				if (this.lookahead == 0)
				{
					break;
				}
			}
			if (this.lookahead >= DeflateManager.MIN_MATCH)
			{
				this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + (DeflateManager.MIN_MATCH - 1)] & 0xFF)) & this.hash_mask;
				hash_head = this.head[this.ins_h] & 0xFFFF;
				this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
				this.head[this.ins_h] = (short)this.strstart;
			}
			this.prev_length = this.match_length;
			this.prev_match = this.match_start;
			this.match_length = DeflateManager.MIN_MATCH - 1;
			if (hash_head != 0 && this.prev_length < this.config.MaxLazy && ((this.strstart - hash_head) & 0xFFFF) <= this.w_size - DeflateManager.MIN_LOOKAHEAD)
			{
				if (this.compressionStrategy != CompressionStrategy.HuffmanOnly)
				{
					this.match_length = this.longest_match(hash_head);
				}
				if (this.match_length <= 5 && (this.compressionStrategy == CompressionStrategy.Filtered || (this.match_length == DeflateManager.MIN_MATCH && this.strstart - this.match_start > 4096)))
				{
					this.match_length = DeflateManager.MIN_MATCH - 1;
				}
			}
			if (this.prev_length >= DeflateManager.MIN_MATCH && this.match_length <= this.prev_length)
			{
				int max_insert = this.strstart + this.lookahead - DeflateManager.MIN_MATCH;
				bool bflush = this._tr_tally(this.strstart - 1 - this.prev_match, this.prev_length - DeflateManager.MIN_MATCH);
				this.lookahead -= this.prev_length - 1;
				this.prev_length -= 2;
				do
				{
					if (++this.strstart <= max_insert)
					{
						this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[this.strstart + (DeflateManager.MIN_MATCH - 1)] & 0xFF)) & this.hash_mask;
						hash_head = this.head[this.ins_h] & 0xFFFF;
						this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
						this.head[this.ins_h] = (short)this.strstart;
					}
				}
				while (--this.prev_length != 0);
				this.match_available = 0;
				this.match_length = DeflateManager.MIN_MATCH - 1;
				this.strstart++;
				if (bflush)
				{
					this.flush_block_only(eof: false);
					if (this._codec.AvailableBytesOut == 0)
					{
						return BlockState.NeedMore;
					}
				}
			}
			else if (this.match_available != 0)
			{
				if (this._tr_tally(0, this.window[this.strstart - 1] & 0xFF))
				{
					this.flush_block_only(eof: false);
				}
				this.strstart++;
				this.lookahead--;
				if (this._codec.AvailableBytesOut == 0)
				{
					return BlockState.NeedMore;
				}
			}
			else
			{
				this.match_available = 1;
				this.strstart++;
				this.lookahead--;
			}
		}
		if (this.match_available != 0)
		{
			bool bflush = this._tr_tally(0, this.window[this.strstart - 1] & 0xFF);
			this.match_available = 0;
		}
		this.flush_block_only(flush == FlushType.Finish);
		if (this._codec.AvailableBytesOut == 0)
		{
			if (flush == FlushType.Finish)
			{
				return BlockState.FinishStarted;
			}
			return BlockState.NeedMore;
		}
		if (flush != FlushType.Finish)
		{
			return BlockState.BlockDone;
		}
		return BlockState.FinishDone;
	}

	internal int longest_match(int cur_match)
	{
		int chain_length = this.config.MaxChainLength;
		int scan = this.strstart;
		int best_len = this.prev_length;
		int limit = ((this.strstart > this.w_size - DeflateManager.MIN_LOOKAHEAD) ? (this.strstart - (this.w_size - DeflateManager.MIN_LOOKAHEAD)) : 0);
		int niceLength = this.config.NiceLength;
		int wmask = this.w_mask;
		int strend = this.strstart + DeflateManager.MAX_MATCH;
		byte scan_end2 = this.window[scan + best_len - 1];
		byte scan_end = this.window[scan + best_len];
		if (this.prev_length >= this.config.GoodLength)
		{
			chain_length >>= 2;
		}
		if (niceLength > this.lookahead)
		{
			niceLength = this.lookahead;
		}
		do
		{
			int match = cur_match;
			if (this.window[match + best_len] != scan_end || this.window[match + best_len - 1] != scan_end2 || this.window[match] != this.window[scan] || this.window[++match] != this.window[scan + 1])
			{
				continue;
			}
			scan += 2;
			match++;
			while (this.window[++scan] == this.window[++match] && this.window[++scan] == this.window[++match] && this.window[++scan] == this.window[++match] && this.window[++scan] == this.window[++match] && this.window[++scan] == this.window[++match] && this.window[++scan] == this.window[++match] && this.window[++scan] == this.window[++match] && this.window[++scan] == this.window[++match] && scan < strend)
			{
			}
			int len = DeflateManager.MAX_MATCH - (strend - scan);
			scan = strend - DeflateManager.MAX_MATCH;
			if (len > best_len)
			{
				this.match_start = cur_match;
				best_len = len;
				if (len >= niceLength)
				{
					break;
				}
				scan_end2 = this.window[scan + best_len - 1];
				scan_end = this.window[scan + best_len];
			}
		}
		while ((cur_match = this.prev[cur_match & wmask] & 0xFFFF) > limit && --chain_length != 0);
		if (best_len <= this.lookahead)
		{
			return best_len;
		}
		return this.lookahead;
	}

	internal int Initialize(ZlibCodec codec, CompressionLevel level)
	{
		return this.Initialize(codec, level, 15);
	}

	internal int Initialize(ZlibCodec codec, CompressionLevel level, int bits)
	{
		return this.Initialize(codec, level, bits, DeflateManager.MEM_LEVEL_DEFAULT, CompressionStrategy.Default);
	}

	internal int Initialize(ZlibCodec codec, CompressionLevel level, int bits, CompressionStrategy compressionStrategy)
	{
		return this.Initialize(codec, level, bits, DeflateManager.MEM_LEVEL_DEFAULT, compressionStrategy);
	}

	internal int Initialize(ZlibCodec codec, CompressionLevel level, int windowBits, int memLevel, CompressionStrategy strategy)
	{
		this._codec = codec;
		this._codec.Message = null;
		if (windowBits < 9 || windowBits > 15)
		{
			throw new ZlibException("windowBits must be in the range 9..15.");
		}
		if (memLevel < 1 || memLevel > DeflateManager.MEM_LEVEL_MAX)
		{
			throw new ZlibException($"memLevel must be in the range 1.. {DeflateManager.MEM_LEVEL_MAX}");
		}
		this._codec.dstate = this;
		this.w_bits = windowBits;
		this.w_size = 1 << this.w_bits;
		this.w_mask = this.w_size - 1;
		this.hash_bits = memLevel + 7;
		this.hash_size = 1 << this.hash_bits;
		this.hash_mask = this.hash_size - 1;
		this.hash_shift = (this.hash_bits + DeflateManager.MIN_MATCH - 1) / DeflateManager.MIN_MATCH;
		this.window = new byte[this.w_size * 2];
		this.prev = new short[this.w_size];
		this.head = new short[this.hash_size];
		this.lit_bufsize = 1 << memLevel + 6;
		this.pending = new byte[this.lit_bufsize * 4];
		this._distanceOffset = this.lit_bufsize;
		this._lengthOffset = 3 * this.lit_bufsize;
		this.compressionLevel = level;
		this.compressionStrategy = strategy;
		this.Reset();
		return 0;
	}

	internal void Reset()
	{
		this._codec.TotalBytesIn = (this._codec.TotalBytesOut = 0L);
		this._codec.Message = null;
		this.pendingCount = 0;
		this.nextPending = 0;
		this.Rfc1950BytesEmitted = false;
		this.status = (this.WantRfc1950HeaderBytes ? DeflateManager.INIT_STATE : DeflateManager.BUSY_STATE);
		this._codec._Adler32 = Adler.Adler32(0u, null, 0, 0);
		this.last_flush = 0;
		this._InitializeTreeData();
		this._InitializeLazyMatch();
	}

	internal int End()
	{
		if (this.status != DeflateManager.INIT_STATE && this.status != DeflateManager.BUSY_STATE && this.status != DeflateManager.FINISH_STATE)
		{
			return -2;
		}
		this.pending = null;
		this.head = null;
		this.prev = null;
		this.window = null;
		if (this.status != DeflateManager.BUSY_STATE)
		{
			return 0;
		}
		return -3;
	}

	private void SetDeflater()
	{
		switch (this.config.Flavor)
		{
		case DeflateFlavor.Store:
			this.DeflateFunction = DeflateNone;
			break;
		case DeflateFlavor.Fast:
			this.DeflateFunction = DeflateFast;
			break;
		case DeflateFlavor.Slow:
			this.DeflateFunction = DeflateSlow;
			break;
		}
	}

	internal int SetParams(CompressionLevel level, CompressionStrategy strategy)
	{
		int result = 0;
		if (this.compressionLevel != level)
		{
			Config newConfig = Config.Lookup(level);
			if (newConfig.Flavor != this.config.Flavor && this._codec.TotalBytesIn != 0L)
			{
				result = this._codec.Deflate(FlushType.Partial);
			}
			this.compressionLevel = level;
			this.config = newConfig;
			this.SetDeflater();
		}
		this.compressionStrategy = strategy;
		return result;
	}

	internal int SetDictionary(byte[] dictionary)
	{
		int length = dictionary.Length;
		int index = 0;
		if (dictionary == null || this.status != DeflateManager.INIT_STATE)
		{
			throw new ZlibException("Stream error.");
		}
		this._codec._Adler32 = Adler.Adler32(this._codec._Adler32, dictionary, 0, dictionary.Length);
		if (length < DeflateManager.MIN_MATCH)
		{
			return 0;
		}
		if (length > this.w_size - DeflateManager.MIN_LOOKAHEAD)
		{
			length = this.w_size - DeflateManager.MIN_LOOKAHEAD;
			index = dictionary.Length - length;
		}
		Array.Copy(dictionary, index, this.window, 0, length);
		this.strstart = length;
		this.block_start = length;
		this.ins_h = this.window[0] & 0xFF;
		this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[1] & 0xFF)) & this.hash_mask;
		for (int i = 0; i <= length - DeflateManager.MIN_MATCH; i++)
		{
			this.ins_h = ((this.ins_h << this.hash_shift) ^ (this.window[i + (DeflateManager.MIN_MATCH - 1)] & 0xFF)) & this.hash_mask;
			this.prev[i & this.w_mask] = this.head[this.ins_h];
			this.head[this.ins_h] = (short)i;
		}
		return 0;
	}

	internal int Deflate(FlushType flush)
	{
		if (this._codec.OutputBuffer == null || (this._codec.InputBuffer == null && this._codec.AvailableBytesIn != 0) || (this.status == DeflateManager.FINISH_STATE && flush != FlushType.Finish))
		{
			this._codec.Message = DeflateManager._ErrorMessage[4];
			throw new ZlibException($"Something is fishy. [{this._codec.Message}]");
		}
		if (this._codec.AvailableBytesOut == 0)
		{
			this._codec.Message = DeflateManager._ErrorMessage[7];
			throw new ZlibException("OutputBuffer is full (AvailableBytesOut == 0)");
		}
		int old_flush = this.last_flush;
		this.last_flush = (int)flush;
		if (this.status == DeflateManager.INIT_STATE)
		{
			int header = DeflateManager.Z_DEFLATED + (this.w_bits - 8 << 4) << 8;
			int level_flags = (int)((this.compressionLevel - 1) & (CompressionLevel)255) >> 1;
			if (level_flags > 3)
			{
				level_flags = 3;
			}
			header |= level_flags << 6;
			if (this.strstart != 0)
			{
				header |= DeflateManager.PRESET_DICT;
			}
			header += 31 - header % 31;
			this.status = DeflateManager.BUSY_STATE;
			this.pending[this.pendingCount++] = (byte)(header >> 8);
			this.pending[this.pendingCount++] = (byte)header;
			if (this.strstart != 0)
			{
				this.pending[this.pendingCount++] = (byte)((this._codec._Adler32 & 0xFF000000u) >> 24);
				this.pending[this.pendingCount++] = (byte)((this._codec._Adler32 & 0xFF0000) >> 16);
				this.pending[this.pendingCount++] = (byte)((this._codec._Adler32 & 0xFF00) >> 8);
				this.pending[this.pendingCount++] = (byte)(this._codec._Adler32 & 0xFFu);
			}
			this._codec._Adler32 = Adler.Adler32(0u, null, 0, 0);
		}
		if (this.pendingCount != 0)
		{
			this._codec.flush_pending();
			if (this._codec.AvailableBytesOut == 0)
			{
				this.last_flush = -1;
				return 0;
			}
		}
		else if (this._codec.AvailableBytesIn == 0 && (int)flush <= old_flush && flush != FlushType.Finish)
		{
			return 0;
		}
		if (this.status == DeflateManager.FINISH_STATE && this._codec.AvailableBytesIn != 0)
		{
			this._codec.Message = DeflateManager._ErrorMessage[7];
			throw new ZlibException("status == FINISH_STATE && _codec.AvailableBytesIn != 0");
		}
		if (this._codec.AvailableBytesIn != 0 || this.lookahead != 0 || (flush != 0 && this.status != DeflateManager.FINISH_STATE))
		{
			BlockState bstate = this.DeflateFunction(flush);
			if (bstate == BlockState.FinishStarted || bstate == BlockState.FinishDone)
			{
				this.status = DeflateManager.FINISH_STATE;
			}
			switch (bstate)
			{
			case BlockState.NeedMore:
			case BlockState.FinishStarted:
				if (this._codec.AvailableBytesOut == 0)
				{
					this.last_flush = -1;
				}
				return 0;
			case BlockState.BlockDone:
				if (flush == FlushType.Partial)
				{
					this._tr_align();
				}
				else
				{
					this._tr_stored_block(0, 0, eof: false);
					if (flush == FlushType.Full)
					{
						for (int i = 0; i < this.hash_size; i++)
						{
							this.head[i] = 0;
						}
					}
				}
				this._codec.flush_pending();
				if (this._codec.AvailableBytesOut == 0)
				{
					this.last_flush = -1;
					return 0;
				}
				break;
			}
		}
		if (flush != FlushType.Finish)
		{
			return 0;
		}
		if (!this.WantRfc1950HeaderBytes || this.Rfc1950BytesEmitted)
		{
			return 1;
		}
		this.pending[this.pendingCount++] = (byte)((this._codec._Adler32 & 0xFF000000u) >> 24);
		this.pending[this.pendingCount++] = (byte)((this._codec._Adler32 & 0xFF0000) >> 16);
		this.pending[this.pendingCount++] = (byte)((this._codec._Adler32 & 0xFF00) >> 8);
		this.pending[this.pendingCount++] = (byte)(this._codec._Adler32 & 0xFFu);
		this._codec.flush_pending();
		this.Rfc1950BytesEmitted = true;
		if (this.pendingCount == 0)
		{
			return 1;
		}
		return 0;
	}
}
