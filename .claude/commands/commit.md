---
description: Suggest git commit messages with gitmoji
---

Analyze the current git changes and suggest commit messages with gitmoji.

Requirements:
1. Run `git status` and `git diff --stat` to see changes
2. Analyze what was changed
3. Suggest exactly 2 commit message options:
   - **간략 버전**: 핵심 변경사항만 한 줄로 요약
   - **디테일 버전**: 제목 + 본문으로 변경 내용을 구체적으로 설명
4. Use conventional commit format: `<gitmoji> <type>: <subject>`

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
- 🧹 Chore

Output format:
```
## 간략 버전
<gitmoji> <type>: <subject>

## 디테일 버전
<gitmoji> <type>: <subject>

<body with bullet points explaining changes>
```
