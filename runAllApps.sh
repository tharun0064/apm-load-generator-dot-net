#!/bin/bash
#
# Run / stop / check the .NET APM load-generator apps.
#
#   ./runAllApps.sh            Start all apps in the background
#   ./runAllApps.sh --status   Show which apps are running (APM / port / PID)
#   ./runAllApps.sh --stop     Stop all apps
#
# Apps 1-3 are instrumented with the New Relic .NET agent; App4 is the
# no-APM baseline. Each app's HTTP port is read from its own .env (API_PORT).

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT_DIR"

# Colors
RED=$'\033[0;31m'; GREEN=$'\033[0;32m'; YELLOW=$'\033[1;33m'
CYAN=$'\033[0;36m'; BOLD=$'\033[1m'; NC=$'\033[0m'

# App registry: "dir|label|apm(yes|no)"
APPS=(
  "App1OltpLoadGenerator|OLTP Load Generator|yes"
  "App2AnalyticsLoadGenerator|Analytics Load Generator|yes"
  "App3AnalyticsLoadGenerator|Analytics Load Generator #3|yes"
  "App4AnalyticsLoadGeneratorNoApm|Analytics (No-APM baseline)|no"
)

hr()    { printf '%s\n' "──────────────────────────────────────────────────────────────────"; }
title() { printf '\n%s%s%s\n' "$BOLD" "$1" "$NC"; hr; }

# HTTP port for an app, read from its .env (falls back to "?")
get_port() {
  local p
  p=$(grep -E '^API_PORT=' "$1/.env" 2>/dev/null | head -1 | cut -d= -f2- | tr -d '\r ')
  printf '%s' "${p:-?}"
}

# PID of a running app. The app runs as an apphost executable whose path
# contains "<dir>/bin", so match on that (NOT "dotnet.*", which never matches
# the apphost and gave false "not running" results).
get_pid() { pgrep -f "$1/bin" 2>/dev/null | head -1; }

print_row() {  # label apm port pid state color
  printf '  %-30s %-4s %-7s %-8s %s%s%s\n' "$1" "$2" "$3" "$4" "$6" "$5" "$NC"
}

status_table() {
  print_row "APP" "APM" "PORT" "PID" "STATE" ""
  hr
  local dir label apm port pid
  for entry in "${APPS[@]}"; do
    IFS='|' read -r dir label apm <<< "$entry"
    port=$(get_port "$dir"); pid=$(get_pid "$dir")
    if [ -n "$pid" ]; then
      print_row "$label" "$apm" "$port" "$pid" "running" "$GREEN"
    else
      print_row "$label" "$apm" "$port" "-" "stopped" "$RED"
    fi
  done
}

# ---------------------------------------------------------------- stop
if [ "${1:-}" = "--stop" ]; then
  title "Stopping all applications"
  for entry in "${APPS[@]}"; do
    IFS='|' read -r dir label apm <<< "$entry"
    pid=$(get_pid "$dir")
    if [ -n "$pid" ]; then
      pkill -f "$dir/bin" 2>/dev/null
      printf '  %s✓%s %-30s stopped (was PID %s)\n' "$GREEN" "$NC" "$label" "$pid"
    else
      printf '  %s•%s %-30s not running\n' "$YELLOW" "$NC" "$label"
    fi
  done
  echo
  exit 0
fi

# ---------------------------------------------------------------- status
if [ "${1:-}" = "--status" ]; then
  title "Application status"
  status_table
  echo
  exit 0
fi

# ---------------------------------------------------------------- start
if ! command -v dotnet &>/dev/null; then
  printf '%sERROR: .NET SDK not found — install the .NET 8 SDK.%s\n' "$RED" "$NC"
  exit 1
fi

title "APM Load Generator — starting all apps"

# Stop anything already running so ports are free.
for entry in "${APPS[@]}"; do
  IFS='|' read -r dir label apm <<< "$entry"
  pid=$(get_pid "$dir")
  if [ -n "$pid" ]; then
    pkill -f "$dir/bin" 2>/dev/null
    printf '  %s•%s stopped stale %s (PID %s)\n' "$YELLOW" "$NC" "$label" "$pid"
  fi
done

# Verify .env files exist.
missing=0
for entry in "${APPS[@]}"; do
  IFS='|' read -r dir label apm <<< "$entry"
  if [ ! -f "$dir/.env" ]; then
    printf '  %s✗%s %s/.env missing — run: cp %s/.env.example %s/.env\n' "$RED" "$NC" "$dir" "$dir" "$dir"
    missing=1
  fi
done
if [ "$missing" = 1 ]; then
  printf '\nCreate the missing .env file(s) with your DB creds + New Relic key, then re-run.\n'
  exit 1
fi

# Build + launch one app. The launch runs in an isolated subshell so the
# New Relic profiler env vars never leak between apps (that isolation is what
# keeps App4 uninstrumented).
start_app() {
  local dir="$1" label="$2" apm="$3" port; port=$(get_port "$dir")
  printf '\n%s▶ %s%s  (APM: %s · port %s)\n' "$CYAN" "$label" "$NC" "$apm" "$port"
  if ! ( cd "$ROOT_DIR/$dir" && dotnet build >/dev/null 2>&1 ); then
    printf '  %s✗ build failed — not started%s\n' "$RED" "$NC"
    return 0
  fi
  (
    cd "$ROOT_DIR/$dir"
    [ -f .env ] && export $(grep -v '^#' .env | xargs)
    if [ "$apm" = "yes" ]; then
      source "$ROOT_DIR/newrelic-env.sh"
      enable_newrelic "$PWD" >/dev/null 2>&1 || true
    fi
    exec dotnet run --no-build >/dev/null 2>&1
  ) &
  printf '  %s✓ launched%s (PID %s)\n' "$GREEN" "$NC" "$!"
}

for entry in "${APPS[@]}"; do
  IFS='|' read -r dir label apm <<< "$entry"
  start_app "$dir" "$label" "$apm"
  sleep 3
done

printf '\n%sWaiting for apps to initialize...%s\n' "$YELLOW" "$NC"
sleep 6

title "Verification"
status_table

# Overall result + URLs
failed=0
for entry in "${APPS[@]}"; do
  IFS='|' read -r dir label apm <<< "$entry"
  [ -z "$(get_pid "$dir")" ] && failed=1
done
echo
if [ "$failed" = 0 ]; then
  printf '%s✓ All applications are running.%s\n' "$GREEN" "$NC"
else
  printf '%s⚠ Some apps are not running.%s Run one in the foreground to see the error:\n' "$RED" "$NC"
  printf '    cd <AppDir> && ./run.sh\n'
fi

cat <<EOF

Endpoints:
$(for entry in "${APPS[@]}"; do IFS='|' read -r dir label apm <<< "$entry"; printf '  %-30s http://localhost:%s\n' "$label" "$(get_port "$dir")"; done)

Commands:
  Status    ./runAllApps.sh --status
  Stop all  ./runAllApps.sh --stop
  Processes ps aux | grep -E 'App[1-4].*/bin' | grep -v grep

New Relic UI → APM & Services: App1/App2/App3 report; App4 is the no-APM baseline.
App logs go to /dev/null — monitor throughput and database activity in New Relic.
EOF
