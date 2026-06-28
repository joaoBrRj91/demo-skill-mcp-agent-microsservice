#!/usr/bin/env bash
input=$(cat)
echo "$input" | grep -q 'git commit' && touch /tmp/jl_guardrails_pending || exit 0
