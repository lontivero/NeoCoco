// DFA.cs -- Generation of the Scanner Automaton
// Compiler Generator NeoCoco/R,
// Copyright (c) 1990, 2004 Hanspeter Moessenboeck, University of Linz
// extended by M. Loeberbauer & A. Woess, Univ. of Linz
// with improvements by Pat Terry, Rhodes University
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation; either version 2, or (at your option) any
// later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
//
// As an exception, it is allowed to write an extension of NeoCoco/R that is
// used as a plugin in non-free software.
//
// If not otherwise stated, any source code generated by NeoCoco/R (other than
// NeoCoco/R itself) does not fall under the GNU General Public License.
// -------------------------------------------------------------------------
using System;

namespace NeoCoco
{
	public class CharSet
	{

		public class Range
		{
			public int from, to;
			public Range next;
			public Range(int from, int to)
			{
				this.from = from;
				this.to = to;
			}
		}

		public Range head;

		public bool this[int i]
		{
			get
			{
				for (Range p = head; p != null; p = p.next)
					if (i < p.from) return false;
					else if (i <= p.to) return true; // p.from <= i <= p.to
				return false;
			}
		}

		public void Set(int i)
		{
			Range cur = head, prev = null;
			while (cur != null && i >= cur.from - 1)
			{
				if (i <= cur.to + 1)
				{ // (cur.from-1) <= i <= (cur.to+1)
					if (i == cur.from - 1) cur.from--;
					else if (i == cur.to + 1)
					{
						cur.to++;
						Range next = cur.next;
						if (next != null && cur.to == next.from - 1) { cur.to = next.to; cur.next = next.next; };
					}
					return;
				}
				prev = cur; cur = cur.next;
			}
			Range n = new Range(i, i);
			n.next = cur;
			if (prev == null) head = n; else prev.next = n;
		}

		public CharSet Clone()
		{
			CharSet s = new CharSet();
			Range prev = null;
			for (Range cur = head; cur != null; cur = cur.next)
			{
				Range r = new Range(cur.from, cur.to);
				if (prev == null) s.head = r; else prev.next = r;
				prev = r;
			}
			return s;
		}

		public bool Equals(CharSet s)
		{
			Range p = head, q = s.head;
			while (p != null && q != null)
			{
				if (p.from != q.from || p.to != q.to) return false;
				p = p.next; q = q.next;
			}
			return p == q;
		}

		public int Elements()
		{
			int n = 0;
			for (Range p = head; p != null; p = p.next) n += p.to - p.from + 1;
			return n;
		}

		public int First()
		{
			if (head != null) return head.from;
			return -1;
		}

		public void Or(CharSet s)
		{
			for (Range p = s.head; p != null; p = p.next)
				for (int i = p.from; i <= p.to; i++) Set(i);
		}

		public void And(CharSet s)
		{
			CharSet x = new CharSet();
			for (Range p = head; p != null; p = p.next)
				for (int i = p.from; i <= p.to; i++)
					if (s[i]) x.Set(i);
			head = x.head;
		}

		public void Subtract(CharSet s)
		{
			CharSet x = new CharSet();
			for (Range p = head; p != null; p = p.next)
				for (int i = p.from; i <= p.to; i++)
					if (!s[i]) x.Set(i);
			head = x.head;
		}

		public bool Includes(CharSet s)
		{
			for (Range p = s.head; p != null; p = p.next)
				for (int i = p.from; i <= p.to; i++)
					if (!this[i]) return false;
			return true;
		}

		public bool Intersects(CharSet s)
		{
			for (Range p = s.head; p != null; p = p.next)
				for (int i = p.from; i <= p.to; i++)
					if (this[i]) return true;
			return false;
		}

		public void Fill()
		{
			head = new Range(Char.MinValue, Char.MaxValue);
		}
	}
}
