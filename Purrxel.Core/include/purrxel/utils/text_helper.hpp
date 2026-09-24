#ifndef TEXT_HELPER_HPP
#define TEXT_HELPER_HPP

#include "pch/std.hpp"

namespace TextHelper
{
	bool IsLetterOrDigit(char c);

	bool MatchPair(const char* text, size_t index, size_t length, char current, char first, char second);

	size_t FindEnd(size_t start, const char* text, size_t length, bool(*selector)(char));

	size_t FindEndOfLine(const char* text, size_t offset, size_t length);

	size_t FindWordBoundary(const char* text, size_t offset, size_t length);

	size_t FindOperatorBoundary(const char* text, size_t offset, size_t length, const std::unordered_set<char>& delimiter);

	bool LookAhead(const char* text, size_t offset, size_t length, char target, size_t& trackedLength, size_t& lines);

	bool LookAhead(const char* text, size_t offset, size_t length, const std::string& target, size_t& trackedLength, size_t& lines);

	size_t CountLeadingWhitespace(const char* text, size_t length);

	size_t CountLeadingWhitespace(const char* text);

	size_t CountTrailingWhitespace(const char* text, size_t length);

	void SkipLeadingWhitespace(const char* text, size_t& index, size_t length);

	void SkipLeadingWhitespace(const char*& text, size_t length);

	void SkipTrailingWhitespace(const char* text, size_t index, size_t& length);

	void SkipTrailingWhitespace(const char* text, size_t& length);

	bool TryParseIdentifier(const char* text, size_t offset, size_t length, size_t& trackedLength);

	bool ConvertToUTF8(const char* data, size_t length, std::string& result);
}
#endif