# Release channel automation snippets

## GitHub Actions
```yaml
# Aggiunge input manuale per selezionare il canale di rilascio senza toccare i job esistenti
on:
  push:
    branches: [ main ]
    paths-ignore:
      - 'ReleaseNotes/**'
  workflow_dispatch:
    inputs:
      release_channel:
        description: 'Select release channel'
        type: choice
        options: [alpha, beta, rc, stable]
        default: alpha

jobs:
  release:
    runs-on: ubuntu-latest
    # Mantiene invariati job/step di build ma introduce la variabile unica richiesta
    env:
      RELEASE_CHANNEL: ${{ github.event.inputs.release_channel || vars.RELEASE_CHANNEL || env.RELEASE_CHANNEL || 'alpha' }}
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      # ... (build/test invariati)

      - name: Determine release channel metadata
        id: release_meta
        shell: bash
        env:
          INPUT_CHANNEL: ${{ env.RELEASE_CHANNEL }}
          SOURCE_VERSION: ${{ vars.SOURCE_VERSION || steps.semver.outputs.new_version }}
          DEFAULT_BASE: ${{ steps.semver.outputs.new_version }}
          BUILD_NUMBER: ${{ env.BUILD_NUMBER || github.run_number }}
          LAST_TAG: ${{ steps.semver.outputs.last_tag }}
        run: |
          set -euo pipefail
          CHANNEL="${INPUT_CHANNEL:-alpha}"
          case "${CHANNEL}" in
            alpha|beta|rc|stable) ;;
            *) echo "Unsupported RELEASE_CHANNEL '${CHANNEL}'" >&2; exit 1;;
          esac

          BASE_VERSION="${SOURCE_VERSION:-${DEFAULT_BASE}}"
          BUILD="${BUILD_NUMBER:-${GITHUB_RUN_NUMBER:-0}}"
          if [[ "${CHANNEL}" == "stable" ]]; then
            TAG_NAME="v${BASE_VERSION}"
            IS_PRERELEASE="false"
            IS_LATEST="true"
            RELEASE_NAME="🚀 v${BASE_VERSION}"
          else
            [[ -n "${BUILD}" ]] || { echo "Missing build identifier" >&2; exit 1; }
            TAG_NAME="v${BASE_VERSION}-${CHANNEL}.${BUILD}"
            IS_PRERELEASE="true"
            IS_LATEST="false"
            RELEASE_NAME="🚀 v${BASE_VERSION}-${CHANNEL}.${BUILD}"
          fi

          OWNER="${GITHUB_REPOSITORY%%/*}"
          NAME="${GITHUB_REPOSITORY#*/}"
          latest_tag() {
            local pattern="$1"
            local tags=()
            mapfile -t tags < <(git tag --list "${pattern}" --sort=-version:refname || true)
            for tag in "${tags[@]}"; do
              if [[ "${tag}" != "${TAG_NAME}" ]]; then
                echo "${tag}"
                return 0
              fi
            done
          }

          PREV_TAG=""
          case "${CHANNEL}" in
            stable)
              PREV_TAG="$(latest_tag 'v[0-9]*.[0-9]*.[0-9]*')"
              ;;
            *)
              pattern="v*-${CHANNEL}.*"
              PREV_TAG="$(latest_tag "${pattern}")"
              if [[ -z "${PREV_TAG}" ]]; then
                PREV_TAG="$(latest_tag 'v[0-9]*.[0-9]*.[0-9]*')"
              fi
              ;;
          esac
          if [[ -z "${PREV_TAG}" ]]; then
            PREV_TAG="${LAST_TAG:-v0.1.0}"
          fi

          COMPARE_URL="https://github.com/${OWNER}/${NAME}/compare/${PREV_TAG}...${TAG_NAME}"

          {
            echo "release_channel=${CHANNEL}"
            echo "version_base=${BASE_VERSION}"
            echo "build_num=${BUILD}"
            echo "tag_name=${TAG_NAME}"
            echo "release_name=${RELEASE_NAME}"
            echo "is_prerelease=${IS_PRERELEASE}"
            echo "is_latest=${IS_LATEST}"
            echo "compare_url=${COMPARE_URL}"
            echo "prev_tag=${PREV_TAG}"
          } >> "$GITHUB_OUTPUT"

      - name: Generate final release notes (Markdown)
        id: relnotes
        env:
          PROJECT_NAME: Momentum
          RELEASE_NAME: ${{ steps.release_meta.outputs.release_name }}
          RELEASE_CHANNEL: ${{ steps.release_meta.outputs.release_channel }}
          IS_PRERELEASE: ${{ steps.release_meta.outputs.is_prerelease }}
          TAG_NAME: ${{ steps.release_meta.outputs.tag_name }}
          PREV_TAG: ${{ steps.release_meta.outputs.prev_tag }}
          COMPARE_URL: ${{ steps.release_meta.outputs.compare_url }}
        run: |
          set -euo pipefail
          mkdir -p "${ARTIFACTS_DIR}" ReleaseNotes
          OUT_ART="${ARTIFACTS_DIR}/RELEASE_NOTES.md"
          OUT_REPO="ReleaseNotes/${TAG_NAME}.md"
          RAW_NOTES="${ARTIFACTS_DIR}/CHANGELOG_RAW.md"
          export OUT_ART OUT_REPO RAW_NOTES
          python tools/release_notes/generate_release_notes.py \
            --base "${PREV_TAG}" \
            --head "HEAD" \
            --version "${TAG_NAME}" \
            --out "${RAW_NOTES}"
          python - <<'PY'
import os
import re
import subprocess
from pathlib import Path

project_name = os.environ.get("PROJECT_NAME", "Momentum")
release_name = os.environ["RELEASE_NAME"]
channel = os.environ["RELEASE_CHANNEL"]
tag_name = os.environ["TAG_NAME"]
prev_tag = os.environ["PREV_TAG"]
compare_url = os.environ["COMPARE_URL"]
raw_notes_path = Path(os.environ["RAW_NOTES"])
out_art = Path(os.environ["OUT_ART"])
out_repo = Path(os.environ["OUT_REPO"])

is_prerelease = os.environ.get("IS_PRERELEASE", "false")
commit_range = f"{prev_tag}..HEAD"
fmt = "%H%x1f%an%x1f%ad%x1f%s%x1f%b%x1e"
try:
    log_out = subprocess.check_output([
        "git",
        "log",
        commit_range,
        f"--pretty=format:{fmt}",
        "--date=short",
    ], text=True)
except subprocess.CalledProcessError:
    log_out = ""

entries = []
if log_out:
    for rec in log_out.split("\x1e"):
        rec = rec.strip()
        if not rec:
            continue
        parts = rec.split("\x1f")
        if len(parts) < 5:
            continue
        sha, author, date, subject, body = parts[:5]
        subject = subject.strip()
        body = (body or "").strip()
        entries.append({
            "sha": sha,
            "author": author,
            "date": date,
            "subject": subject,
            "body": body,
        })

conv_re = re.compile(r"^(?P<type>[a-zA-Z]+)(?:\((?P<scope>[^)]+)\))?(?P<bang>!)?:\s*(?P<msg>.+)$")

def classify(commit):
    m = conv_re.match(commit["subject"])
    ctype = m.group("type").lower() if m else "other"
    scope = m.group("scope") if m else None
    msg = m.group("msg") if m else commit["subject"]
    breaking = bool(m and m.group("bang")) or "BREAKING CHANGE" in commit["body"]
    return ctype, scope, msg.strip(), breaking

def format_line(commit, scope, msg):
    prefix = f"**{scope}**: " if scope else ""
    return f"- {prefix}{msg} ({commit['sha'][:7]}) by {commit['author']}"

highlights = []
changes = []
fixes = []
breaking = []

for commit in entries:
    ctype, scope, msg, breaking_flag = classify(commit)
    line = format_line(commit, scope, msg)
    if ctype == "feat":
        if len(highlights) < 5:
            highlights.append(line)
        changes.append(line)
    elif ctype == "fix":
        fixes.append(line)
    else:
        changes.append(line)
    if breaking_flag:
        breaking.append(line)

def ensure_content(lines):
    return lines if lines else ["- _No entries recorded._"]

header = [
    f"# {project_name} — {release_name}",
    f"Channel: {channel}",
    f"Prerelease: {is_prerelease}",
    f"Commit range: {prev_tag}..{tag_name}",
    "",
]

section_map = [
    ("Highlights", ensure_content(highlights)),
    ("Changes", ensure_content(changes)),
    ("Fixes", ensure_content(fixes)),
    ("Breaking changes", ensure_content(breaking)),
]

body_lines = []
for title, lines in section_map:
    body_lines.append(f"## {title}")
    body_lines.extend(lines)
    body_lines.append("")

body_lines.append("## Changelog diff")
body_lines.append(compare_url)
body_lines.append("")

raw_appendix = []
if raw_notes_path.exists():
    raw_text = raw_notes_path.read_text(encoding="utf-8").strip()
    if raw_text:
        raw_appendix.extend(["---", "## Detailed changelog", "", raw_text])

final_text = "\n".join(header + body_lines + raw_appendix).rstrip() + "\n"
out_art.write_text(final_text, encoding="utf-8")
out_repo.write_text(final_text, encoding="utf-8")
PY
          echo "artifacts_file=${OUT_ART}" >> "$GITHUB_OUTPUT"
          echo "repo_file=${OUT_REPO}" >> "$GITHUB_OUTPUT"

      - name: Create tag on main
        env:
          TAG: ${{ steps.release_meta.outputs.tag_name }}
        run: |
          git tag -a "${TAG}" "$(git rev-parse origin/main)" -m "Release ${TAG}" || true
          git push origin "${TAG}"

      - name: Create GitHub Release (draft) on main
        uses: softprops/action-gh-release@v2
        with:
          tag_name: ${{ steps.release_meta.outputs.tag_name }}
          name: ${{ steps.release_meta.outputs.release_name }}
          prerelease: ${{ steps.release_meta.outputs.is_prerelease }}
          make_latest: ${{ steps.release_meta.outputs.is_latest }}
          draft: true
          generate_release_notes: false
          body_path: ${{ steps.relnotes.outputs.artifacts_file }}
```

## Azure DevOps
```yaml
# Parametro runtime per scegliere il canale senza rinominare job/stage
parameters:
  - name: releaseChannel
    displayName: Release channel
    type: string
    default: alpha
    values:
      - alpha
      - beta
      - rc
      - stable

variables:
  # Default a alpha ma permetti override da variabili di pipeline
  - name: RELEASE_CHANNEL
    value: ${{ parameters.releaseChannel }}
  # SOURCE_VERSION può essere impostata da GitVersion o variabile di pipeline
  - name: SOURCE_VERSION
    value: $[coalesce(variables.SOURCE_VERSION, '$(SOURCE_VERSION)', '1.0.0')]
  - name: BUILD_NUMBER
    value: $[coalesce(variables.BUILD_NUMBER, '$(Build.BuildId)')]

steps:
  # ... (build/test invariati)

  - bash: |
      set -euo pipefail
      CHANNEL="${RELEASE_CHANNEL:-alpha}"
      case "${CHANNEL}" in
        alpha|beta|rc|stable) ;;
        *) echo "Unsupported RELEASE_CHANNEL '${CHANNEL}'" >&2; exit 1;;
      esac

      BASE_VERSION="${SOURCE_VERSION}"
      BUILD="${BUILD_NUMBER}"
      if [[ "${CHANNEL}" == "stable" ]]; then
        TAG_NAME="v${BASE_VERSION}"
        IS_PRERELEASE="false"
        IS_LATEST="true"
        RELEASE_NAME="🚀 v${BASE_VERSION}"
      else
        [[ -n "${BUILD}" ]] || { echo "Missing build identifier" >&2; exit 1; }
        TAG_NAME="v${BASE_VERSION}-${CHANNEL}.${BUILD}"
        IS_PRERELEASE="true"
        IS_LATEST="false"
        RELEASE_NAME="🚀 v${BASE_VERSION}-${CHANNEL}.${BUILD}"
      fi

      latest_tag() {
        local pattern="$1"
        local tags=()
        mapfile -t tags < <(git tag --list "${pattern}" --sort=-version:refname || true)
        for tag in "${tags[@]}"; do
          if [[ "${tag}" != "${TAG_NAME}" ]]; then
            echo "${tag}"
            return 0
          fi
        done
      }

      PREV_TAG=""
      case "${CHANNEL}" in
        stable)
          PREV_TAG="$(latest_tag 'v[0-9]*.[0-9]*.[0-9]*')"
          ;;
        *)
          pattern="v*-${CHANNEL}.*"
          PREV_TAG="$(latest_tag "${pattern}")"
          if [[ -z "${PREV_TAG}" ]]; then
            PREV_TAG="$(latest_tag 'v[0-9]*.[0-9]*.[0-9]*')"
          fi
          ;;
      esac
      if [[ -z "${PREV_TAG}" ]]; then
        PREV_TAG="$(git describe --tags --abbrev=0 2>/dev/null || echo v0.1.0)"
      fi

      COMPARE_URL="https://github.com/${REPO_OWNER}/${REPO_NAME}/compare/${PREV_TAG}...${TAG_NAME}"

      {
        echo "##vso[task.setvariable variable=RELEASE_CHANNEL;isOutput=true]${CHANNEL}"
        echo "##vso[task.setvariable variable=VERSION_BASE;isOutput=true]${BASE_VERSION}"
        echo "##vso[task.setvariable variable=BUILD_NUM;isOutput=true]${BUILD}"
        echo "##vso[task.setvariable variable=TAG_NAME;isOutput=true]${TAG_NAME}"
        echo "##vso[task.setvariable variable=RELEASE_NAME;isOutput=true]${RELEASE_NAME}"
        echo "##vso[task.setvariable variable=IS_PRERELEASE;isOutput=true]${IS_PRERELEASE}"
        echo "##vso[task.setvariable variable=IS_LATEST;isOutput=true]${IS_LATEST}"
        echo "##vso[task.setvariable variable=PREV_TAG;isOutput=true]${PREV_TAG}"
        echo "##vso[task.setvariable variable=COMPARE_URL;isOutput=true]${COMPARE_URL}"
      }
    name: DetermineReleaseMetadata
    displayName: Determine release channel metadata
    env:
      RELEASE_CHANNEL: $(RELEASE_CHANNEL)
      SOURCE_VERSION: $(SOURCE_VERSION)
      BUILD_NUMBER: $(BUILD_NUMBER)
      REPO_OWNER: $(REPO_OWNER)
      REPO_NAME: $(REPO_NAME)

  - bash: |
      set -euo pipefail
      mkdir -p $(Build.ArtifactStagingDirectory)
      OUT_ART="$(Build.ArtifactStagingDirectory)/RELEASE_NOTES.md"
      RAW_NOTES="$(Build.ArtifactStagingDirectory)/CHANGELOG_RAW.md"
      python tools/release_notes/generate_release_notes.py \
        --base "$(DetermineReleaseMetadata.PREV_TAG)" \
        --head "HEAD" \
        --version "$(DetermineReleaseMetadata.TAG_NAME)" \
        --out "${RAW_NOTES}"
      python - <<'PY'
import os
import re
import subprocess
from pathlib import Path

project_name = os.environ.get("PROJECT_NAME", "Momentum")
release_name = os.environ["RELEASE_NAME"]
channel = os.environ["RELEASE_CHANNEL"]
is_prerelease = os.environ.get("IS_PRERELEASE", "false")
tag_name = os.environ["TAG_NAME"]
prev_tag = os.environ["PREV_TAG"]
compare_url = os.environ["COMPARE_URL"]
raw_notes_path = Path(os.environ["RAW_NOTES"])
out_art = Path(os.environ["OUT_ART"])

commit_range = f"{prev_tag}..HEAD"
fmt = "%H%x1f%an%x1f%ad%x1f%s%x1f%b%x1e"
try:
    log_out = subprocess.check_output([
        "git", "log", commit_range, f"--pretty=format:{fmt}", "--date=short"
    ], text=True)
except subprocess.CalledProcessError:
    log_out = ""

entries = []
if log_out:
    for rec in log_out.split("\x1e"):
        rec = rec.strip()
        if not rec:
            continue
        parts = rec.split("\x1f")
        if len(parts) < 5:
            continue
        sha, author, date, subject, body = parts[:5]
        subject = subject.strip()
        body = (body or "").strip()
        entries.append({
            "sha": sha,
            "author": author,
            "date": date,
            "subject": subject,
            "body": body,
        })

conv_re = re.compile(r"^(?P<type>[a-zA-Z]+)(?:\((?P<scope>[^)]+)\))?(?P<bang>!)?:\s*(?P<msg>.+)$")

def classify(commit):
    m = conv_re.match(commit["subject"])
    ctype = m.group("type").lower() if m else "other"
    scope = m.group("scope") if m else None
    msg = m.group("msg") if m else commit["subject"]
    breaking = bool(m and m.group("bang")) or "BREAKING CHANGE" in commit["body"]
    return ctype, scope, msg.strip(), breaking

def format_line(commit, scope, msg):
    prefix = f"**{scope}**: " if scope else ""
    return f"- {prefix}{msg} ({commit['sha'][:7]}) by {commit['author']}"

highlights = []
changes = []
fixes = []
breaking = []

for commit in entries:
    ctype, scope, msg, breaking_flag = classify(commit)
    line = format_line(commit, scope, msg)
    if ctype == "feat":
        if len(highlights) < 5:
            highlights.append(line)
        changes.append(line)
    elif ctype == "fix":
        fixes.append(line)
    else:
        changes.append(line)
    if breaking_flag:
        breaking.append(line)

def ensure_content(lines):
    return lines if lines else ["- _No entries recorded._"]

header = [
    f"# {project_name} — {release_name}",
    f"Channel: {channel}",
    f"Prerelease: {is_prerelease}",
    f"Commit range: {prev_tag}..{tag_name}",
    "",
]

section_map = [
    ("Highlights", ensure_content(highlights)),
    ("Changes", ensure_content(changes)),
    ("Fixes", ensure_content(fixes)),
    ("Breaking changes", ensure_content(breaking)),
]

body_lines = []
for title, lines in section_map:
    body_lines.append(f"## {title}")
    body_lines.extend(lines)
    body_lines.append("")

body_lines.append("## Changelog diff")
body_lines.append(compare_url)
body_lines.append("")

raw_appendix = []
if raw_notes_path.exists():
    raw_text = raw_notes_path.read_text(encoding="utf-8").strip()
    if raw_text:
        raw_appendix.extend(["---", "## Detailed changelog", "", raw_text])

final_text = "\n".join(header + body_lines + raw_appendix).rstrip() + "\n"
out_art.write_text(final_text, encoding="utf-8")
PY
    name: GenerateReleaseNotes
    displayName: Generate release notes template
    env:
      PROJECT_NAME: Momentum
      RELEASE_NAME: $(DetermineReleaseMetadata.RELEASE_NAME)
      RELEASE_CHANNEL: $(DetermineReleaseMetadata.RELEASE_CHANNEL)
      IS_PRERELEASE: $(DetermineReleaseMetadata.IS_PRERELEASE)
      TAG_NAME: $(DetermineReleaseMetadata.TAG_NAME)
      PREV_TAG: $(DetermineReleaseMetadata.PREV_TAG)
      COMPARE_URL: $(DetermineReleaseMetadata.COMPARE_URL)
      OUT_ART: $(Build.ArtifactStagingDirectory)/RELEASE_NOTES.md
      RAW_NOTES: $(Build.ArtifactStagingDirectory)/CHANGELOG_RAW.md

  - task: GitTag@1
    displayName: Create tag on main
    inputs:
      tag: $(DetermineReleaseMetadata.TAG_NAME)
      message: Release $(DetermineReleaseMetadata.TAG_NAME)

  - task: GitHubRelease@1
    displayName: Publish GitHub release
    inputs:
      gitHubConnection: <service-connection>
      repositoryName: $(REPO_OWNER)/$(REPO_NAME)
      action: edit
      tagSource: manual
      tag: $(DetermineReleaseMetadata.TAG_NAME)
      title: $(DetermineReleaseMetadata.RELEASE_NAME)
      isDraft: true
      isPreRelease: $(DetermineReleaseMetadata.IS_PRERELEASE)
      isLatest: $(DetermineReleaseMetadata.IS_LATEST)
      releaseNotesFile: $(Build.ArtifactStagingDirectory)/RELEASE_NOTES.md
      assets: $(Build.ArtifactStagingDirectory)/*.zip
```
