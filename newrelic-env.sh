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

    # Locate the agent home by search rather than a hardcoded
    # bin/Debug/net8.0/newrelic path, so this survives Debug/Release and
    # TFM/RID changes.
    nr_home=$(find "$app_dir/bin" -type d -name newrelic 2>/dev/null | head -1)

    if [ -z "$nr_home" ]; then
        echo "WARNING: New Relic agent home not found under $app_dir/bin — build the app first. APM disabled."
        return 1
    fi

    # Use the profiler that sits directly in the agent home (this is the one that
    # matches CORECLR_NEWRELIC_HOME and its adjacent Core/extensions). Only fall
    # back to an arch subdir copy if the root one is missing. A bare
    # `find ... | head -1` is non-deterministic and can grab a subdir copy that
    # fails to initialize (no profiler log, agent never connects).
    if [ -f "$nr_home/libNewRelicProfiler.so" ]; then
        nr_profiler="$nr_home/libNewRelicProfiler.so"
    else
        nr_profiler=$(find "$nr_home" -name libNewRelicProfiler.so 2>/dev/null | head -1)
    fi

    if [ -z "$nr_profiler" ]; then
        echo "WARNING: libNewRelicProfiler.so not found under $nr_home — build the app first. APM disabled."
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

    # Do not write agent/profiler logs to disk. The profiler otherwise creates a
    # NewRelic.Profiler.<pid>.log per launch (one per restart) that never gets
    # cleaned up and fills the disk. NEW_RELIC_LOG_ENABLED=false is the master
    # switch that stops both agent and profiler file logging. Set to "true" to
    # re-enable when you need to diagnose the agent.
    export NEW_RELIC_LOG_ENABLED=false

    echo "New Relic .NET agent enabled: $nr_home"
}

disable_newrelic() {
    unset CORECLR_ENABLE_PROFILING CORECLR_PROFILER CORECLR_NEWRELIC_HOME \
          CORECLR_PROFILER_PATH NEW_RELIC_LOG_ENABLED
}
