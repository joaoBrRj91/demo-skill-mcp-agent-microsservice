#!/usr/bin/env bash
BUILD_RC=0

if [ -f /tmp/jl_cs_modified ]; then
  rm /tmp/jl_cs_modified
  BUILD_OUT=$(cd "/c/Users/joaon/Projetos/IA/Study Projects/demo-skill-mcp-server-net-core/JL.Commerce.Tecnology.Service" \
    && dotnet build --no-restore -v q 2>&1)
  BUILD_RC=$?
  echo "$BUILD_OUT" | tail -15
fi

if [ -f /tmp/jl_guardrails_pending ]; then
  rm /tmp/jl_guardrails_pending
  echo "Git commit detected -- automatically run the validate-guardrails-implementation agent now."
  exit 2
fi

exit $BUILD_RC
