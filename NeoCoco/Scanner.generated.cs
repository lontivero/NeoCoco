
using System;
using System.IO;
using System.Collections.Generic;

namespace NeoCoco {


//-----------------------------------------------------------------------------------
// Scanner
//-----------------------------------------------------------------------------------
public partial class Scanner
{
	const int MaxTerminal = 41;
	const int noSym = 41;


	static Scanner()
	{
		start = new Dictionary<int, int>(128);
				for (int i = 65; i <= 90; ++i) start[i] = 1;
		for (int i = 95; i <= 95; ++i) start[i] = 1;
		for (int i = 97; i <= 122; ++i) start[i] = 1;
		for (int i = 48; i <= 57; ++i) start[i] = 2;
		start[34] = 12; 
		start[39] = 5; 
		start[36] = 13; 
		start[61] = 16; 
		start[46] = 31; 
		start[43] = 17; 
		start[45] = 18; 
		start[60] = 32; 
		start[62] = 20; 
		start[124] = 23; 
		start[40] = 33; 
		start[41] = 24; 
		start[91] = 25; 
		start[93] = 26; 
		start[123] = 27; 
		start[125] = 28; 
		start[Buffer.EOF] = -1;

	}

	void Casing1()
	{
		
	}

	void Casing2()
	{
					tval[tlen++] = (char) ch;
	}



	bool Comment0() {
		int level = 1, pos0 = pos, line0 = line, col0 = col, charPos0 = charPos;
		NextCh();
		if (ch == '/') {
			NextCh();
			for(;;) {
				if (ch == 10) {
					level--;
					if (level == 0) { oldEols = line - line0; NextCh(); return true; }
					NextCh();
				} else if (ch == Buffer.EOF) return false;
				else NextCh();
			}
		} else {
			buffer.Pos = pos0; NextCh(); line = line0; col = col0; charPos = charPos0;
		}
		return false;
	}

	bool Comment1() {
		int level = 1, pos0 = pos, line0 = line, col0 = col, charPos0 = charPos;
		NextCh();
		if (ch == '*') {
			NextCh();
			for(;;) {
				if (ch == '*') {
					NextCh();
					if (ch == '/') {
						level--;
						if (level == 0) { oldEols = line - line0; NextCh(); return true; }
						NextCh();
					}
				} else if (ch == '/') {
					NextCh();
					if (ch == '*') {
						level++; NextCh();
					}
				} else if (ch == Buffer.EOF) return false;
				else NextCh();
			}
		} else {
			buffer.Pos = pos0; NextCh(); line = line0; col = col0; charPos = charPos0;
		}
		return false;
	}


	void CheckLiteral()
	{
		var value = lastToken.val;
		switch (value) {
			case "COMPILER": lastToken.kind = 6; break;
			case "IGNORECASE": lastToken.kind = 7; break;
			case "CHARACTERS": lastToken.kind = 8; break;
			case "TOKENS": lastToken.kind = 9; break;
			case "PRAGMAS": lastToken.kind = 10; break;
			case "COMMENTS": lastToken.kind = 11; break;
			case "FROM": lastToken.kind = 12; break;
			case "TO": lastToken.kind = 13; break;
			case "NESTED": lastToken.kind = 14; break;
			case "IGNORE": lastToken.kind = 15; break;
			case "PRODUCTIONS": lastToken.kind = 16; break;
			case "END": lastToken.kind = 19; break;
			case "ANY": lastToken.kind = 23; break;
			case "WEAK": lastToken.kind = 29; break;
			case "SYNC": lastToken.kind = 36; break;
			case "IF": lastToken.kind = 37; break;
			case "CONTEXT": lastToken.kind = 38; break;
			default: break;
		}
	}

	Token NextToken()
	{
		while (ch == ' ' ||
			ch >= 9 && ch <= 10 || ch == 13
		) NextCh();
		if (ch == '/' && Comment0() ||ch == '/' && Comment1()) return NextToken();
		int recKind = noSym;
		int recEnd = pos;
		lastToken = new Token();
		lastToken.pos = pos; lastToken.col = col; lastToken.line = line; lastToken.charPos = charPos;
		int state;
		state = start.ContainsKey(ch) ? start[ch] : 0;
		tlen = 0; AddCh();

		switch (state)
		{
			case -1: { lastToken.kind = eofSym; break; } // NextCh already done
			case 0:
				{
					if (recKind != noSym)
					{
						tlen = recEnd - lastToken.pos;
						SetScannerBehindT();
					}
					lastToken.kind = recKind; break;
				} // NextCh already done
							case 1:
				recEnd = pos; recKind = 1;
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 1;}
				else {lastToken.kind = 1; lastToken.val = new String(tval, 0, tlen); CheckLiteral(); return lastToken;}
			case 2:
				recEnd = pos; recKind = 2;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 2;}
				else {lastToken.kind = 2; break;}
			case 3:
				{lastToken.kind = 3; break;}
			case 4:
				{lastToken.kind = 4; break;}
			case 5:
				if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '&' || ch >= '(' && ch <= '[' || ch >= ']' && ch <= 65535) {AddCh(); goto case 6;}
				else if (ch == 92) {AddCh(); goto case 7;}
				else {goto case 0;}
			case 6:
				if (ch == 39) {AddCh(); goto case 9;}
				else {goto case 0;}
			case 7:
				if (ch >= ' ' && ch <= '~') {AddCh(); goto case 8;}
				else {goto case 0;}
			case 8:
				if (ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f') {AddCh(); goto case 8;}
				else if (ch == 39) {AddCh(); goto case 9;}
				else {goto case 0;}
			case 9:
				{lastToken.kind = 5; break;}
			case 10:
				recEnd = pos; recKind = 42;
				if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 10;}
				else {lastToken.kind = 42; break;}
			case 11:
				recEnd = pos; recKind = 43;
				if (ch >= '-' && ch <= '.' || ch >= '0' && ch <= ':' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 11;}
				else {lastToken.kind = 43; break;}
			case 12:
				if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '!' || ch >= '#' && ch <= '[' || ch >= ']' && ch <= 65535) {AddCh(); goto case 12;}
				else if (ch == 10 || ch == 13) {AddCh(); goto case 4;}
				else if (ch == '"') {AddCh(); goto case 3;}
				else if (ch == 92) {AddCh(); goto case 14;}
				else {goto case 0;}
			case 13:
				recEnd = pos; recKind = 42;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 10;}
				else if (ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 15;}
				else {lastToken.kind = 42; break;}
			case 14:
				if (ch >= ' ' && ch <= '~') {AddCh(); goto case 12;}
				else {goto case 0;}
			case 15:
				recEnd = pos; recKind = 42;
				if (ch >= '0' && ch <= '9') {AddCh(); goto case 10;}
				else if (ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z') {AddCh(); goto case 15;}
				else if (ch == '=') {AddCh(); goto case 11;}
				else {lastToken.kind = 42; break;}
			case 16:
				{lastToken.kind = 17; break;}
			case 17:
				{lastToken.kind = 20; break;}
			case 18:
				{lastToken.kind = 21; break;}
			case 19:
				{lastToken.kind = 22; break;}
			case 20:
				{lastToken.kind = 25; break;}
			case 21:
				{lastToken.kind = 26; break;}
			case 22:
				{lastToken.kind = 27; break;}
			case 23:
				{lastToken.kind = 28; break;}
			case 24:
				{lastToken.kind = 31; break;}
			case 25:
				{lastToken.kind = 32; break;}
			case 26:
				{lastToken.kind = 33; break;}
			case 27:
				{lastToken.kind = 34; break;}
			case 28:
				{lastToken.kind = 35; break;}
			case 29:
				{lastToken.kind = 39; break;}
			case 30:
				{lastToken.kind = 40; break;}
			case 31:
				recEnd = pos; recKind = 18;
				if (ch == '.') {AddCh(); goto case 19;}
				else if (ch == '>') {AddCh(); goto case 22;}
				else if (ch == ')') {AddCh(); goto case 30;}
				else {lastToken.kind = 18; break;}
			case 32:
				recEnd = pos; recKind = 24;
				if (ch == '.') {AddCh(); goto case 21;}
				else {lastToken.kind = 24; break;}
			case 33:
				recEnd = pos; recKind = 30;
				if (ch == '.') {AddCh(); goto case 29;}
				else {lastToken.kind = 30; break;}

		}
		lastToken.val = new String(tval, 0, tlen);
		return lastToken;
	}
}

}