#!/bin/bash
#
# Shared helper to enable the New Relic .NET agent (CoreCLR profiler) for an app.
#
# The NewRelic.Agent NuGet package copies the agent + profiler into the build
# output ("newrelic" folder), but the CoreCLR runtime only loads the profiler
# when the CORECLR_* environment variables below are set. Source this file and
# call enable_newrelic AFTER the app has been built.
#
#   source /path/to/newrelic-env.sh
#   enable_newrelic "/abs/path/to/AppDir"
#
# The agent authenticates with NEW_RELIC_LICENSE_KEY and reports under
# NEW_RELIC_APP_NAME; both env vars override newrelic.config, so no secret needs
# to live in the config file. Profiler is Linux-only (libNewRelicProfiler.so).

# CoreCLR profiler CLSID for the New Relic .NET agent (fixed, do not change).
NEWRELIC_PROFILER_CLSID='{36032161-FFC0-4B61-B559-F6C5D41BAE5A}'

enable_newrelic() {
    local app_dir="${1:-$PWD}"
    local nr_home nr_profiler

    # Locate the agent home and profiler by search rather than a hardcoded
    # bin/Debug/net8.0/newrelic path, so this survives Debug/Release and
    # TFM/RID changes.
    nr_home=$(find "$app_dir/bin" -type d -name newrelic 2>/dev/null | head -1)
    nr_profiler=$(find "$app_dir/bin" -name libNewRelicProfiler.so 2>/dev/null | head -1)

    if [ -z "$nr_home" ] || [ -z "$nr_profiler" ]; then
        echo "WARNING: New Relic agent files not found under $app_dir/bin — build the app first. APM disabled."
        return 1
    fi

    # Prefer this app's tuned newrelic.config over the package default.
    if [ -f "$app_dir/newrelic.config" ]; then
        cp -f "$app_dir/newrelic.config" "$nr_home/newrelic.config"
    fi

    export CORECLR_ENABLE_PROFILING=1
    export CORECLR_PROFILER="$NEWRELIC_PROFILER_CLSID"
    export CORECLR_NEWRELIC_HOME="$nr_home"
    export CORECLR_PROFILER_PATH="$nr_profiler"

    echo "New Relic .NET agent enabled: $nr_home"
}

disable_newrelic() {
    unset CORECLR_ENABLE_PROFILING CORECLR_PROFILER CORECLR_NEWRELIC_HOME CORECLR_PROFILER_PATH
}
