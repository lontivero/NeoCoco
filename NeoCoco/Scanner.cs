
using System;
using System.IO;
using System.Collections.Generic;

namespace NeoCoco {

public enum TokenKind
{
	COMPILER  = 6,
	IGNORECASE  = 7,
	CHARACTERS  = 8,
	TOKENS  = 9,
	PRAGMAS  = 10,
	COMMENTS  = 11,
	FROM  = 12,
	TO  = 13,
	NESTED  = 14,
	IGNORE  = 15,
	PRODUCTIONS  = 16,
	END  = 19,
	ANY  = 23,
	WEAK  = 29,
	SYNC  = 36,
	IF  = 37,
	CONTEXT  = 38,
}

public class Token
{
	public int kind;     // token kind
	public int pos;      // token position in bytes in the source text (starting at 0)
	public int charPos;  // token position in characters in the source text (starting at 0)
	public int col;      // token column (starting at 1)
	public int line;     // token line (starting at 1)
	public string val = "";   // token value
	public Token next;   // ML 2005-03-11 Tokens are kept in linked list
}


//-----------------------------------------------------------------------------------
// Scanner
//-----------------------------------------------------------------------------------
public partial class Scanner
{
	private const char EOL = '\n';
	private const int eofSym = 0; /* pdt */


	public Buffer buffer; // scanner buffer

	Token lastToken;          // current token
	int ch;           // current input character
	int pos;          // byte position of current character
	int charPos;      // position by unicode characters starting with 0
	int col;          // column number of current character
	int line;         // line number of current character
	int oldEols;      // EOLs that appeared in a comment;
	static readonly Dictionary<int, int> start; // maps first token character to start state

	Token tokens;     // list of tokens already peeked (first token is a dummy)
	Token pt;         // current peek token

	char[] tval = new char[128]; // text of current token
	int tlen;         // length of current token

	public Scanner(string fileName)
	{
		try
		{
			var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			buffer = new Buffer(stream, false);
			Init();
		}
		catch (IOException)
		{
			throw new FatalError("Cannot open file " + fileName);
		}
	}

	public Scanner(Stream s)
	{
		buffer = new Buffer(s, true);
		Init();
	}

	void Init()
	{
		pos = -1; line = 1; col = 0; charPos = -1;
		oldEols = 0;
		NextCh();
		if (ch == 0xEF)
		{ // check optional byte order mark for UTF-8
			NextCh(); int ch1 = ch;
			NextCh(); int ch2 = ch;
			if (ch1 != 0xBB || ch2 != 0xBF)
			{
				throw new FatalError(String.Format("illegal byte order mark: EF {0,2:X} {1,2:X}", ch1, ch2));
			}
			buffer = new UTF8Buffer(buffer); col = 0; charPos = -1;
			NextCh();
		}
		pt = tokens = new Token();  // first token is a dummy
	}

	void NextCh()
	{
		if (oldEols > 0) { ch = EOL; oldEols--; }
		else
		{
			pos = buffer.Pos;
			// buffer reads unicode chars, if UTF8 has been detected
			ch = buffer.Read(); col++; charPos++;
			// replace isolated '\r' by '\n' in order to make
			// eol handling uniform across Windows, Unix and Mac
			if (ch == '\r' && buffer.Peek() != '\n') ch = EOL;
			if (ch == EOL) { line++; col = 0; }
		}
		Casing1();
	}

	void AddCh()
	{
		if (tlen >= tval.Length)
		{
			char[] newBuf = new char[2 * tval.Length];
			Array.Copy(tval, 0, newBuf, 0, tval.Length);
			tval = newBuf;
		}
		if (ch != Buffer.EOF)
		{
			Casing2();
			NextCh();
		}
	}

	private void SetScannerBehindT()
	{
		buffer.Pos = lastToken.pos;
		NextCh();
		line = lastToken.line; col = lastToken.col; charPos = lastToken.charPos;
		for (int i = 0; i < tlen; i++) NextCh();
	}

	// get the next token (possibly a token already seen during peeking)
	public Token Scan()
	{
		if (tokens.next == null)
		{
			return NextToken();
		}
		else
		{
			pt = tokens = tokens.next;
			return tokens;
		}
	}

	// peek for the next token, ignore pragmas
	public Token Peek()
	{
		do
		{
			if (pt.next == null)
			{
				pt.next = NextToken();
			}
			pt = pt.next;
		} while (pt.kind > MaxTerminal); // skip pragmas

		return pt;
	}

	// make sure that peeking starts at the current scan position
	public void ResetPeek() { pt = tokens; }

} // end Scanner

}