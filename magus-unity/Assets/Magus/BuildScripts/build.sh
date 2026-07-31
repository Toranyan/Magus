#!/usr/bin/env bash
# Entry point for local/CI builds on macOS/Linux.
# Usage: ./build.sh <platform> <configuration>
#   platform:      android, ios, windows
#   configuration: development, release
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$(cd "$SCRIPT_DIR/../../.." && pwd)"

PLATFORM="${1:-}"
CONFIGURATION="${2:-}"

usage() {
    echo "Usage: $0 <platform> <configuration>"
    echo "  platform:      android, ios, windows"
    echo "  configuration: development, release"
}

if [[ -z "$PLATFORM" || -z "$CONFIGURATION" ]]; then
    usage
    exit 1
fi

case "$PLATFORM" in
    android) PLATFORM_METHOD="Android" ;;
    ios) PLATFORM_METHOD="IOS" ;;
    windows) PLATFORM_METHOD="Windows" ;;
    *)
        echo "Error: unknown platform '$PLATFORM'. Expected android, ios, or windows." >&2
        exit 1
        ;;
esac

case "$CONFIGURATION" in
    development) CONFIGURATION_METHOD="Development" ;;
    release) CONFIGURATION_METHOD="Release" ;;
    *)
        echo "Error: unknown configuration '$CONFIGURATION'. Expected development or release." >&2
        exit 1
        ;;
esac

BUILD_METHOD="magus.build.MagusBuild.Build${PLATFORM_METHOD}${CONFIGURATION_METHOD}"
UNITY_EXECUTABLE="${UNITY_PATH:-unity}"

LOG_DIR="$PROJECT_PATH/Build/Logs"
mkdir -p "$LOG_DIR"
LOG_FILE="$LOG_DIR/${PLATFORM}-${CONFIGURATION}.log"

"$UNITY_EXECUTABLE" \
    -batchmode \
    -quit \
    -projectPath "$PROJECT_PATH" \
    -executeMethod "$BUILD_METHOD" \
    -logFile "$LOG_FILE"

EXIT_CODE=$?
echo "Unity exited with code $EXIT_CODE. Log: $LOG_FILE"
exit $EXIT_CODE
