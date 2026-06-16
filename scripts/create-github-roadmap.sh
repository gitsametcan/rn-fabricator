#!/usr/bin/env bash
set -euo pipefail

REPO="${GITHUB_REPOSITORY:-gitsametcan/rn-fabricator}"
TOKEN="${GH_TOKEN:-${GITHUB_TOKEN:-}}"
API="${GITHUB_API_URL:-https://api.github.com}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ROADMAP_FILE="$ROOT_DIR/.github/roadmap/issues.json"

if [[ -z "$TOKEN" ]]; then
  echo "GH_TOKEN or GITHUB_TOKEN is required." >&2
  exit 1
fi

if [[ ! -f "$ROADMAP_FILE" ]]; then
  echo "Roadmap file not found: $ROADMAP_FILE" >&2
  exit 1
fi

api() {
  local method="$1"
  local path="$2"
  local data="${3:-}"

  if [[ -n "$data" ]]; then
    curl -fsS \
      -X "$method" \
      -H "Accept: application/vnd.github+json" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-GitHub-Api-Version: 2022-11-28" \
      "$API/repos/$REPO$path" \
      -d "$data"
  else
    curl -fsS \
      -X "$method" \
      -H "Accept: application/vnd.github+json" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-GitHub-Api-Version: 2022-11-28" \
      "$API/repos/$REPO$path"
  fi
}

exists_in_array() {
  local value="$1"
  local json="$2"

  jq -e --arg value "$value" '.[] | select(.title == $value or .name == $value)' >/dev/null <<<"$json"
}

echo "Creating labels for $REPO"
jq -c '.labels[]' "$ROADMAP_FILE" | while read -r label; do
  name="$(jq -r '.name' <<<"$label")"
  payload="$(jq -c '{name, color, description}' <<<"$label")"

  if api POST "/labels" "$payload" >/dev/null 2>&1; then
    echo "  created label: $name"
  else
    echo "  label exists or could not be created: $name"
  fi
done

echo "Creating milestones for $REPO"
existing_milestones="$(api GET "/milestones?state=all&per_page=100")"
jq -c '.milestones[]' "$ROADMAP_FILE" | while read -r milestone; do
  title="$(jq -r '.title' <<<"$milestone")"

  if exists_in_array "$title" "$existing_milestones"; then
    echo "  milestone exists: $title"
    continue
  fi

  payload="$(jq -c '{title, description}' <<<"$milestone")"
  api POST "/milestones" "$payload" >/dev/null
  echo "  created milestone: $title"
done

echo "Creating issues for $REPO"
existing_issues="$(api GET "/issues?state=all&per_page=100")"
milestones="$(api GET "/milestones?state=all&per_page=100")"

jq -c '.issues[]' "$ROADMAP_FILE" | while read -r issue; do
  title="$(jq -r '.title' <<<"$issue")"
  milestone_title="$(jq -r '.milestone' <<<"$issue")"

  if exists_in_array "$title" "$existing_issues"; then
    echo "  issue exists: $title"
    continue
  fi

  milestone_number="$(jq -r --arg title "$milestone_title" '.[] | select(.title == $title) | .number' <<<"$milestones")"
  payload="$(jq -c --argjson milestone "$milestone_number" '. + {milestone: $milestone}' <<<"$issue" | jq -c 'del(.milestone | select(type == "string"))')"

  api POST "/issues" "$payload" >/dev/null
  echo "  created issue: $title"
done

echo "Roadmap creation complete."
