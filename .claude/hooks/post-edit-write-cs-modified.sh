#!/usr/bin/env bash
input=$(cat)
echo "$input" | grep -qE '[.]cs"' && touch /tmp/jl_cs_modified || exit 0
