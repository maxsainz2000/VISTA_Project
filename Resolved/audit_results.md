# UX-50 Audit Results

I have reviewed the uncommitted changes from the Claude Code session along with the `Expectation.jpg` reference and the `darkmode_login.png` / `lightmode_login.png` screenshots.

## Verdict: FAILED

The current uncommitted implementation **DOES NOT** pass the success criteria defined in `Plans/VISTA_Modules/Experience/50-login-raster-hero.md`. It failed on several key visual and functional requirements.

### 1. Fails Criterion #3 (Time-of-Day Moods)
> **Requirement:** *`VISTA_LOGIN_SCENE_HOUR` = 10 / 20 / 1 → Dusk / Evening / LateNight... the `GradeOverlay` crossfade is seamless...*
* **What happened:** Claude deleted the previous `LoginScenePhase` mood logic to clean up the vector engine, but completely **failed to add the `GradeOverlay`** replacement. The `UX-50-summary.md` even lists this as an unchecked item in "What's Next". 
* **Result:** The background is statically stuck at Dusk (the raw Canva image), and the time-of-day mood regrading is missing.

### 2. Fails Criterion #4 (Glass Card Blur & Mapping)
> **Requirement:** *Glass card: the lit raster + glows drift blurred behind the form as warm bokeh... DA6 panel growth re-clips/re-maps cleanly...*
* **What happened:** Claude wrapped `SceneRoot` in a `<Viewbox Stretch="UniformToFill">` to scale the new 1600x900 Canva painting to the window. However, `LoginView.xaml`'s frosted glass mapping assumes 1:1 window coordinates. 
* **Result:** The `VisualBrush` samples the 1600x900 `SceneRoot` but is given coordinates from the 900x700 scaled window space. This causes a massive coordinate mismatch. In the screenshots, the background inside the login card is physically **misaligned** with the background outside the card (e.g., the curb line breaks), and the **blur effect is completely broken** (the background passes through the card perfectly sharp).

### 3. Fails Criterion #8 (Verdict Gate & Screenshots)
> **Requirement:** *Both themes × three moods × reduced-motion screenshotted for the summary.*
* **What happened:** Only 2 states were provided (`darkmode` and `lightmode`). Furthermore, because the glass card is not properly blurring or darkening the background, the contrast for the Light Theme text ("VISTA", "Username", "Password") over the bright storefront image is severely compromised. 

---

### Clarifying Questions for You:
1. Shall I proceed to fix the `Viewbox` coordinate mapping in `DynamicSceneCanvas` and `LoginView` so the frosted glass blur and alignment work perfectly?
2. Shall I implement the missing `GradeOverlay` so the Dusk / Evening / LateNight moods are fully functional over the new raster hero?
