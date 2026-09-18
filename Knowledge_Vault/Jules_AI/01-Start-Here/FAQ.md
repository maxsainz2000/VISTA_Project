---
title: FAQ
type: reference
section: Start Here
tags: [jules/setup, jules/security, jules/troubleshooting]
source: https://jules.google/docs/faq/
captured: 2026-09-12
---

# FAQ

**What is Jules?**
A coding agent that fixes bugs, writes docs, updates apps, and implements features. It integrates with GitHub and works autonomously — submit a task, walk away, come back to reviewable code.

**Is it free?**
There is a no-cost tier plus paid tiers via Google AI plans. See [[Plans and Limits]].

**How does it work under the hood?**
Every task runs in a **fresh VM** that clones the repo, installs dependencies, and makes changes from your prompt. Setup scripts ensure the project builds and tests correctly — [[Environment Setup]].

**What about security?**
Code executes in a cloud VM **with internet access**. Google's guidance: treat it like any public or shared compute surface, and remember Jules is an LLM-based system operating on both code and non-code files in the repo.

> [!warning] You are responsible for the code you run
> - Don't commit secrets (API keys, tokens, credentials) to the repo.
> - Avoid known-vulnerable dependencies; GitHub's repository-security quickstart is the recommended baseline.
> - Be cautious with third-party packages and shell commands.

**Can Jules run `npm run dev` or other long-lived commands?**
**No.** Dev servers and watch scripts aren't supported in setup scripts. Use discrete install/test commands. This is one of the most common causes of failure — [[Errors and Failures]].

**Which languages?**
Language-agnostic, but strongest with JavaScript/TypeScript, Python, Go, Java, and Rust. What actually works depends on what's on the VM ([[Preinstalled Tools]]) and the clarity of your setup script.

**Can I leave while it works?**
Yes — that's the point. Enable notifications so you're alerted when a plan is ready or the task finishes.

**How do I report a bug?**
The **feedback** button in the Jules UI. No account or tracker needed; it goes straight to the team. See [[Feedback and Support]].

**What happens if a task fails?**
Jules retries automatically; persistent failures are marked failed and you're notified. Usual culprits: broken setup scripts, vague prompts. Revise and rerun — [[Errors and Failures]].

**How many tasks can I run?**
See [[Plans and Limits]].

**Can I change repo access?**
Yes — GitHub → profile → **Settings → Applications → Google Labs Jules → Configure → Repository access**. Then refresh Jules. Details in [[Managing Tasks and Repos]].

**Does Jules train on private repos?**
**No.** Google states Jules does not train on private repository content, and points to <https://jules.google.com/legal> for how data is used to improve Jules.

**Source:** <https://jules.google/docs/faq/>
