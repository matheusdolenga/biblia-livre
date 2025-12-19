# AI Context Handoff: BibleApp (Bíblia Livre)

**ATTENTION AI AGENT:** If you are reading this file, the user is resuming work on the "BibleApp" project. ALIGN YOURSELF with the context below before taking action.

## 1. Project Overview & Mission
- **Name:** Bíblia Livre (BibleApp).
- **Goal:** A free, offline, secure, and accessible Bible application for Windows (WPF).
- **Target Audience:** Christians in persecuted regions (China, Iran, etc.) or offline environments.
- **Core Values:** 100% Free, No Ads, No Tracking, Offline-First, Secure.

## 2. Technical Architecture
- **Framework:** .NET 9 (Windows Desktop / WPF).
- **Pattern:** MVVM (Model-View-ViewModel) using `CommunityToolkit.Mvvm`.
- **Database:** SQLite (`sqlite-net-pcl`) storing the Bible text and User Data (notes/highlights).
  - `bible.db`: Read-only scripture text.
  - `user.db`: User data (highlights, notes, settings).
- **UI Toolkit:** MaterialDesignInXamlToolkit.
- **Key Files:**
  - `MainViewModel.cs`: Core logic (Navigation, Search, Verse of Day, Reader).
  - `BibleService.cs`: SQLite queries for fetching scripture.
  - `VerseOfDayService.cs`: Curated daily verses logic.

## 3. Current State (v0.5 Alpha - Released)
- **Status:** **STABLE / RELEASED**.
- **Features Completed:**
  - [x] Full Bible Text Reader (PT-BR).
  - [x] Search (Old/New Test, Book Filters).
  - [x] Dark/Light Mode.
  - [x] Highlighting (Yellow) & Notes system.
  - [x] Data Persistence (User settings saved).
  - [x] Text-to-Speech (TTS).
  - [x] Verse of the Day (Dialog + Gift Icon).
- **Environment:**
  - `.gitignore` configured to exclude `bin`, `obj`, logs, and `.env`.
  - GitHub Repo: `https://github.com/matheusdolenga/biblia-livre`.

## 4. Immediate Next Steps (Roadmap)
The user is moving towards **v0.6 Beta**.
1.  **Reading Streak (Ofensiva):** Logic exists in `User` model (`CurrentStreak`), needs UI implementation (Icon + Badge).
2.  **Image Generator:** Feature to create shareable social media images from verses.
3.  **Refinement:** Persistent window state (size/position).

## 5. Instructions for You (The AI)
1.  **Security First:** Never add hardcoded paths or API keys. Keep the app offline-capable.
2.  **Design Matters:** Maintain the premium Material Design aesthetic. Use `MaterialDesignThemes` resources.
3.  **Context Awareness:** Always check `MainViewModel.cs` before creating new Logic to avoid duplication.
4.  **Distribution:** When asked to build, always use `dotnet build -c Release`.

## 6. AUTO-UPDATE PROTOCOL (CRITICAL)
**Instructions for the AI:**
- **WHEN TO UPDATE:** At the end of *every* significant task or session (before calling `notify_user` to finish).
- **WHAT TO UPDATE:** 
  - Update **Current State** (Section 3) if features changed.
  - Update **Immediate Next Steps** (Section 4) if priorities shifted.
  - Update **Last Updated** date below.
- **WHY:** To preserve context in case of unexpected disconnection. **You are responsible for keeping this file alive.**

---
*Last Updated: 19/12/2025 (v0.5 Alpha Published)*
