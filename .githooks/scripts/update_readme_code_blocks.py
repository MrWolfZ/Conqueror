import sys
import re
from pathlib import Path

def update_readme(readme_content: str, replacements: dict) -> str:
    lines = readme_content.splitlines()
    result = []
    i = 0

    while i < len(lines):
        line = lines[i]

        # Match quoted or unquoted REPLACECODE marker
        marker_match = re.match(r'^(?P<quote>>\s*)?<!--\s*REPLACECODE\s+(.+?)\s*-->$', line)
        if marker_match:
            quote = marker_match.group('quote') or ''
            filepath = marker_match.group(2).strip()

            result.append(line)  # keep the marker
            i += 1

            if filepath not in replacements:
                continue

            # Preserve any blank lines after the marker
            while i < len(lines) and lines[i].strip() == '':
                result.append(lines[i])
                i += 1

            # Match start of code block
            if i < len(lines) and re.match(rf'^{re.escape(quote)}```\w*\s*$', lines[i]):
                result.append(lines[i])  # keep ```
                i += 1

                # Skip existing code block
                while i < len(lines):
                    if re.match(rf'^{re.escape(quote)}```\s*$', lines[i]):
                        i += 1  # Skip closing ```
                        break
                    i += 1

                # Insert new content
                for repl_line in replacements[filepath].splitlines():
                    result.append(f"{quote}{repl_line}")
                result.append(f"{quote}```")

            else:
                result.append(f"{quote}<!-- ERROR: Expected ``` block after marker for {filepath} -->")
        else:
            result.append(line)
            i += 1

    return "\n".join(result) + "\n"

if __name__ == "__main__":
    readme_path = Path("README.md")
    content = readme_path.read_text(encoding="utf-8")

    replacements = {}
    for path in sys.argv[1:]:
        with open(path, "r", encoding="utf-8") as f:
            replacements[path] = f.read()
    updated_readme = update_readme(content, replacements)

    readme_path.write_text(updated_readme, encoding="utf-8")
