---
description: Suggest git commit messages with gitmoji
---

Analyze the current git changes and suggest commit messages with gitmoji.

Requirements:
1. Run `git status` and `git diff --stat` to see changes
2. Analyze what was changed
3. Suggest 2-3 commit message options with appropriate gitmoji
4. Use conventional commit format: `<gitmoji> <type>: <subject>`
5. Include a brief body explaining the changes

Common gitmoji to use:
- ✨ New feature
- 🐛 Bug fix
- 📝 Documentation
- 🎨 Code structure
- ♻️ Refactoring
- 🔧 Configuration
- 🚀 Performance
- 🔥 Remove code
- 🚚 Move/rename
- 📦 Dependencies
- 🎉 Initial commit
- 🏗️ Architecture

Provide options in different styles (concise, detailed, feature-focused).
