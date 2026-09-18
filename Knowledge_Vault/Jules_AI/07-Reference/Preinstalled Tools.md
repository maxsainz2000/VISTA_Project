---
title: Preinstalled Tools
type: reference
section: Reference
tags: [jules/environment]
source: https://jules.google/docs/environment/
captured: 2026-09-12
aliases: [Base image, VM toolchain]
---

# Preinstalled Tools

Every Jules VM runs **Ubuntu Linux** with a broad developer toolchain. Headline runtimes: **Node.js, Bun, Python, Go, Java, Rust**.

> [!note] Versions drift
> The list below is the environment-check output published on the docs page at capture time. To see the live inventory, run this in a setup script and click **Run to Validate**:
> ```bash
> set +x; . /opt/environment_summary.sh
> ```

## Python
`python3` 3.12.11 · `pip` 25.1.1 · `pipx` 1.4.3 · `poetry` 2.1.3 · `uv` 0.7.13 · `black` 25.1.0 · `mypy` 1.16.1 · `pytest` 8.4.0 · `ruff` 0.12.0 · `pyenv` (3.10.18, 3.12.11)

## Node.js
`node` v22.16.0 (also v18.20.8, v20.19.2 via `nvm`) · `npm` 11.4.2 · `yarn` 1.22.22 · `pnpm` 10.12.1 · `eslint` v9.29.0 · `prettier` 3.5.3 · `chromedriver` 137.0.7151.70

Bun is supported out of the box (since July 2025). Playwright ships in the base image for front-end verification (since Aug 2025).

## Java
`java` OpenJDK 21.0.7 · `maven` 3.9.10 · `gradle` 8.8

## Go
`go` 1.24.3 linux/amd64

## Rust
`rustc` 1.87.0 · `cargo` 1.87.0

## C / C++
`clang` 18.1.3 · `gcc` 13.3.0 · `cmake` 3.28.3 · `ninja` 1.11.1 · `conan` 2.17.0

## Docker
`docker` 28.2.2 · Docker Compose v2.36.2

## Utilities
`awk` (GNU Awk 5.2.1) · `curl` 8.5.0 · `git` 2.49.0 · `grep` 3.11 · `gzip` 1.12 · `jq` 1.7 · `make` 4.3 · `rg` (ripgrep 14.1.0) · `sed` 4.9 · `tar` 1.35 · `tmux` 3.4 · `yq`

## Machine

- **Disk:** 20 GB (raised Aug 2025)
- **Network:** internet access from inside the VM
- **Lifetime:** short-lived and per-task; snapshots persist setup between tasks ([[Environment Setup]])

**Source:** <https://jules.google/docs/environment/>
