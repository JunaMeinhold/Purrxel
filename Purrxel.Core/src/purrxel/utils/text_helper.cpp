#include "utils/text_helper.hpp"
#include "utils/endianness.hpp"

namespace TextHelper
{
	bool IsLetterOrDigit(char c)
	{
		return std::isalpha(c) || std::isdigit(c);
	}

	bool MatchPair(const char* text, size_t index, size_t length, char current, char first, char second)
	{
		return current == first && index + 1 < length && *(text + index + 1) == second;
	}

	size_t FindEnd(size_t start, const char* text, size_t length, bool (*selector)(char))
	{
		size_t position = start;
		while (position < length && selector(text[position]))
		{
			position++;
		}
		return position;
	}

	size_t FindEndOfLine(const char* text, size_t offset, size_t length)
	{
		const char* current = text + offset;
		const char* end = text + length;

		while (current != end)
		{
			char c = *current;
			bool cr = c == '\r';
			if (cr || c == '\n')
			{
				size_t width = 1;
				if (cr && current + 1 != end && current[1] == '\n')
				{
					width++;
				}
				return (size_t)(current + width - text - offset);
			}
			current++;
		}

		return length - offset;
	}

	size_t FindWordBoundary(const char* text, size_t offset, size_t length)
	{
		const char* current = text + offset;
		const char* end = text + length;

		while (current != end)
		{
			char c = *current;
			if (!IsLetterOrDigit(c) && c != '_')
			{
				return (size_t)(current - text - offset);
			}
			current++;
		}

		return length - offset;
	}

	size_t FindOperatorBoundary(const char* text, size_t offset, size_t length, const std::unordered_set<char>& delimiter)
	{
		const char* current = text + offset;
		const char* end = text + length;

		while (current != end)
		{
			char c = *current;
			if (std::isspace(c) || std::isdigit(c) || std::isalpha(c) || delimiter.find(c) != delimiter.end())
			{
				return (size_t)(current - text - offset);
			}
			current++;
		}

		return length - offset;
	}

	bool LookAhead(const char* text, size_t offset, size_t length, char target, size_t& trackedLength, size_t& lines)
	{
		const char* current = text + offset;
		const char* end = text + length;

		bool escaped = false;
		while (current < end)
		{
			char c = *current;
			if (c == target && !escaped)
			{
				trackedLength = (size_t)(current - text - offset);
				return true;
			}

			bool cr = c == '\r';
			if (cr || c == '\n')
			{
				size_t width = 1;
				if (cr && current + 1 != end && current[1] == '\n')
				{
					width++;
				}
				current += width;
				lines++;
				continue;
			}

			escaped = false;
			if (c == '\\')
			{
				escaped = true;
			}
			current++;
		}
		trackedLength = 0;
		return false;
	}

	bool LookAhead(const char* text, size_t offset, size_t length, const std::string& target, size_t& trackedLength, size_t& lines)
	{
		const char* current = text + offset;
		const char* end = text + length;

		bool escaped = false;
		size_t ix = 0;
		while (current < end)
		{
			char c = *current;
			if (c == target[ix] && !escaped)
			{
				ix++;
				if (ix == target.length())
				{
					trackedLength = (size_t)(current - text - offset);
					return true;
				}
			}
			else
			{
				ix = 0;
			}

			bool cr = c == '\r';
			if (cr || c == '\n')
			{
				size_t width = 1;
				if (cr && current + 1 != end && current[1] == '\n')
				{
					width++;
				}
				current += width;
				lines++;
				continue;
			}

			escaped = false;
			if (c == '\\')
			{
				escaped = true;
			}
			current++;
		}
		trackedLength = 0;
		return false;
	}

	size_t CountLeadingWhitespace(const char* text, size_t length)
	{
		const char* current = text;
		const char* end = text + length;
		while (current != end && std::isspace(*current) && *current != '\n' && *current != '\r')
		{
			current++;
		}
		return (size_t)(current - text);
	}

	size_t CountLeadingWhitespace(const char* text)
	{
		const char* current = text;
		char c;
		while ((c = *current) != '\0' && std::isspace(c))
		{
			current++;
		}
		return (size_t)(current - text);
	}

	size_t CountTrailingWhitespace(const char* text, size_t length)
	{
		const char* current = text + length - 1;
		const char* start = text;
		while (current >= start && std::isspace(*current))
		{
			current--;
		}
		return (size_t)(text + length - 1 - current);
	}

	void SkipLeadingWhitespace(const char* text, size_t& index, size_t length)
	{
		index += CountLeadingWhitespace(text + index, length - index);
	}

	void SkipLeadingWhitespace(const char*& text, size_t length)
	{
		text += CountLeadingWhitespace(text, length);
	}

	void SkipTrailingWhitespace(const char* text, size_t index, size_t& length)
	{
		length -= CountTrailingWhitespace(text + index, length - index);
	}

	void SkipTrailingWhitespace(const char* text, size_t& length)
	{
		length -= CountTrailingWhitespace(text, length);
	}

	bool TryParseIdentifier(const char* text, size_t offset, size_t length, size_t& trackedLength)
	{
		const char* current = text + offset;
		const char* end = text + length;
		if (current == end || !std::isalpha(*current) && *current != '_')
		{
			trackedLength = 0;
			return false;
		}

		current++;
		while (current != end)
		{
			char c = *current;
			if (!IsLetterOrDigit(c) && c != '_')
			{
				break;
			}
			current++;
		}

		trackedLength = (size_t)(current - (text + offset));
		return true;
	}

	static void ConvertUTF16ToUTF8(const char16_t* utf16Chars, size_t utf16Length, bool isLittleEndian, std::string& output)
	{
		char buffer[4]{};

		bool mustConvert = EndianUtils::IsLittleEndian() ^ isLittleEndian;

		const char16_t* end = utf16Chars + utf16Length;
		while (utf16Chars < end)
		{
			char16_t utf16Char = *utf16Chars;
			if (mustConvert)
			{
				utf16Char = EndianUtils::ReverseEndianness(utf16Char);
			}

			size_t len = 0;

			uint32_t codePoint = utf16Char;

			if (codePoint <= 0x7F)
			{
				buffer[0] = static_cast<char>(codePoint);  // 1 byte
				len = 1;
			}
			else if (codePoint <= 0x7FF)
			{
				buffer[0] = static_cast<char>((codePoint >> 6) | 0xC0);  // 2 bytes
				buffer[1] = static_cast<char>((codePoint & 0x3F) | 0x80);
				len = 2;
			}
			else if (codePoint >= 0xD800 && codePoint <= 0xDBFF)
			{
				if (utf16Chars + 1 >= end)
				{
					buffer[0] = static_cast<char>(0xEF);
					buffer[1] = static_cast<char>(0xBF);
					buffer[2] = static_cast<char>(0xBD);
					output.append(buffer, 3);
					return;
				}

				char16_t lowSurrogate = *(utf16Chars + 1);
				if (mustConvert)
				{
					lowSurrogate = EndianUtils::ReverseEndianness(lowSurrogate);
				}
				if (lowSurrogate >= 0xDC00 && lowSurrogate <= 0xDFFF)
				{
					int codePointSurrogate = 0x10000 + ((utf16Char - 0xD800) << 10) + (lowSurrogate - 0xDC00);

					buffer[0] = static_cast<char>(0xF0 | (codePointSurrogate >> 18)); // 4 bytes
					buffer[1] = static_cast<char>(0x80 | ((codePointSurrogate >> 12) & 0x3F));
					buffer[2] = static_cast<char>(0x80 | ((codePointSurrogate >> 6) & 0x3F));
					buffer[3] = static_cast<char>(0x80 | (codePointSurrogate & 0x3F));
					utf16Chars++;
					len = 4;
				}
				else
				{
					buffer[0] = static_cast<char>(0xEF);
					buffer[1] = static_cast<char>(0xBF);
					buffer[2] = static_cast<char>(0xBD);
					len = 3;
				}
			}
			else if (codePoint >= 0xDC00 && codePoint <= 0xDFFF)
			{
				buffer[0] = static_cast<char>(0xEF);
				buffer[1] = static_cast<char>(0xBF);
				buffer[2] = static_cast<char>(0xBD);
				len = 3;
			}
			else
			{
				buffer[0] = static_cast<char>((codePoint >> 12) | 0xE0);  // 3 bytes
				buffer[1] = static_cast<char>(((codePoint >> 6) & 0x3F) | 0x80);
				buffer[2] = static_cast<char>((codePoint & 0x3F) | 0x80);
				len = 3;
			}

			output.append(buffer, len);
			utf16Chars++;
		}
	}

	static void ConvertUTF32ToUTF8(const char32_t* utf32Chars, size_t utf32Length, bool isLittleEndian, std::string& output)
	{
		char buffer[4]{};

		bool mustConvert = EndianUtils::IsLittleEndian() ^ isLittleEndian;

		const char32_t* end = utf32Chars + utf32Length;
		while (utf32Chars < end)
		{
			char32_t utf32Char = *utf32Chars;
			if (mustConvert)
			{
				utf32Char = EndianUtils::ReverseEndianness(utf32Char);
			}

			uint32_t codepoint = utf32Char;

			size_t len;
			if (codepoint <= 0x7F)
			{
				// 1-byte UTF-8
				buffer[0] = (static_cast<char>(codepoint));
				len = 1;
			}
			else if (codepoint <= 0x7FF)
			{
				// 2-byte UTF-8
				buffer[0] = (0xC0 | static_cast<char>(codepoint >> 6));
				buffer[1] = (0x80 | static_cast<char>(codepoint & 0x3F));
				len = 2;
			}
			else if (codepoint <= 0xFFFF)
			{
				// 3-byte UTF-8
				buffer[0] = (0xE0 | static_cast<char>(codepoint >> 12));
				buffer[1] = (0x80 | static_cast<char>((codepoint >> 6) & 0x3F));
				buffer[2] = (0x80 | static_cast<char>(codepoint & 0x3F));
				len = 3;
			}
			else if (codepoint <= 0x10FFFF)
			{
				// 4-byte UTF-8
				buffer[0] = (0xF0 | static_cast<char>(codepoint >> 18));
				buffer[1] = (0x80 | static_cast<char>((codepoint >> 12) & 0x3F));
				buffer[2] = (0x80 | static_cast<char>((codepoint >> 6) & 0x3F));
				buffer[3] = (0x80 | static_cast<char>(codepoint & 0x3F));
				len = 4;
			}
			else
			{
				buffer[0] = static_cast<char>(0xEF);
				buffer[1] = static_cast<char>(0xBF);
				buffer[2] = static_cast<char>(0xBD);
				len = 3;
			}

			output.append(buffer, len);
			utf32Chars++;
		}
	}

	bool ConvertToUTF8(const char* data, size_t length, std::string& result)
	{
		// BOM for UTF-8
		if (length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
		{
			data += 3;
			length -= 3;
			return false;
		}

		if (length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
		{
			data += 2;
			length -= 2;

			size_t numCodeUnits = length / 4;
			std::vector<char16_t> temp(numCodeUnits);
			std::memcpy(temp.data(), data, numCodeUnits * 4);

			ConvertUTF16ToUTF8(temp.data(), numCodeUnits, false, result);
			return true;
		}

		if (length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
		{
			data += 2;
			length -= 2;

			size_t numCodeUnits = length / 4;
			std::vector<char16_t> temp(numCodeUnits);
			std::memcpy(temp.data(), data, numCodeUnits * 4);

			ConvertUTF16ToUTF8(temp.data(), numCodeUnits, true, result);
			return true;
		}

		if (length >= 4 && data[0] == 0x00 && data[1] == 0x00 && data[2] == 0xFE && data[3] == 0xFF)
		{
			data += 4;
			length -= 4;

			size_t numCodeUnits = length / 4;
			std::vector<char32_t> temp(numCodeUnits);
			std::memcpy(temp.data(), data, numCodeUnits * 4);

			ConvertUTF32ToUTF8(temp.data(), numCodeUnits, false, result);
			return true;
		}

		if (length >= 4 && data[0] == 0xFF && data[1] == 0xFE && data[2] == 0x00 && data[3] == 0x00)
		{
			data += 4;
			length -= 4;

			size_t numCodeUnits = length / 4;
			std::vector<char32_t> temp(numCodeUnits);
			std::memcpy(temp.data(), data, numCodeUnits * 4);

			ConvertUTF32ToUTF8(temp.data(), numCodeUnits, true, result);
			return true;
		}

		return false;
	}

	static bool isspace(char32_t c)
	{
		if (c <= 0x20) {
			return c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\f' || c == '\v';
		}

		return (c >= 0x2000 && c <= 0x200A) || // Range for common space characters
			(c == 0x202F) ||                // Narrow no-break space
			(c == 0x3000) ||                // Ideographic space
			(c == 0x205F);                  // Medium Mathematical Space
	}

	static char32_t decodeuni(const char* text, size_t& width, size_t max_len)
	{
		if (!text || max_len == 0)
		{
			width = 0;
			return 0xFFFD;
		}

		uint8_t c1 = static_cast<uint8_t>(text[0]);

		if (c1 <= 0x7F)
		{
			width = 1;
			return c1;
		}

		if ((c1 >> 5) == 0x6)
		{
			if (max_len < 2)
			{
				width = 1;
				return 0xFFFD;
			}

			width = 2;
			return ((c1 & 0x1F) << 6) | (static_cast<uint8_t>(text[1]) & 0x3F);
		}

		if ((c1 >> 4) == 0xE)
		{
			if (max_len < 3)
			{
				width = 1;
				return 0xFFFD;
			}

			width = 3;
			return ((c1 & 0x0F) << 12) | ((static_cast<uint8_t>(text[1]) & 0x3F) << 6) | (static_cast<uint8_t>(text[2]) & 0x3F);
		}

		if ((c1 >> 3) == 0x1E)
		{
			if (max_len < 4)
			{
				width = 1;
				return 0xFFFD;
			}

			width = 4;
			return ((c1 & 0x07) << 18) | ((static_cast<uint8_t>(text[1]) & 0x3F) << 12) | ((static_cast<uint8_t>(text[2]) & 0x3F) << 6) | (static_cast<uint8_t>(text[3]) & 0x3F);
		}

		width = 1;
		return 0xFFFD;
	}
}